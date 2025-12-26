using System.Text.Json.Serialization;
using ve.build.core;
using ve.build.core.projects;
using ve.build.core.tasks;
using File = ve.build.core.files.File;

namespace ve.build.cpp.cpp;

internal struct Dependencies
{
	public Dependencies()
	{
	}
	public Dependencies(Int32 hash, string[] includes, string[] moduels, string[] args)
	{
		this.Hash = hash;
		this.Includes = includes;
		this.Modules = moduels;
		this.CommandLineArgs = args;
	}
	private static int Combine(int seed, int value)
	{
		unchecked
		{
			return seed ^ (value + (int)0x9e3779b9 + (seed << 6) + (seed >> 2));
		}
	}
	private static int GetStableHashCode(string str)
	{
		unchecked
		{
			const int p = 16777619;
			int hash = (int)2166136261;

			foreach (char c in str)
				hash = (hash ^ c) * p;

			return hash;
		}
	}

	[JsonPropertyName("hash")] public Int32 Hash { get; set; } = -1;
	[JsonPropertyName("includes")] public string[] Includes { get; set; } = [];
	[JsonPropertyName("modules")] public string[] Modules { get; set; } = [];
	[JsonPropertyName("commandLineArgs")] public string[] CommandLineArgs { get; set; } = [];

	public static async Task<Dependencies> FromFile(string path)
	{
		if (System.IO.File.Exists(path) == false)
		{
			await new Dependencies().toFile(path);
		}
		return System.Text.Json.JsonSerializer.Deserialize<Dependencies>(await System.IO.File.ReadAllTextAsync(path));
	}
	public static async Task<Dependencies> FromCpp(File cppFile, string[] includes, string[] modules, string[] args)
	{
		var hashFile = GetStableHashCode(await System.IO.File.ReadAllTextAsync(cppFile.Path));
		foreach (var dep in includes.Concat(modules))
		{
			hashFile = Combine(hashFile, GetStableHashCode(await System.IO.File.ReadAllTextAsync(dep)));
		}
		return new Dependencies(hashFile, includes, modules, args);
	}

	public Task toFile(string path)
	{
		var dirName = Path.GetDirectoryName(path);
		if (Directory.Exists(dirName) == false) Directory.CreateDirectory(dirName!);
		return System.IO.File.WriteAllTextAsync(path, System.Text.Json.JsonSerializer.Serialize(this));
	}

	public bool needRebuild(Dependencies other)
	{
		return this.Hash != other.Hash ||
			   this.Includes.Length != other.Includes.Length ||
			   this.Modules.Length != other.Modules.Length ||
			   this.CommandLineArgs.Length != other.CommandLineArgs.Length ||
			   this.Includes.Except(other.Includes).Any() ||
			   this.Modules.Except(other.Modules).Any() ||
			   this.CommandLineArgs.Except(other.CommandLineArgs).Any();
	}
}

public static class ClExtension
{
	private static readonly Dictionary<string, Dictionary<string, string?>> _defines = new();
	private static readonly string[] cppExts = [".c", ".cpp", ".cxx", ".cc", ".c++", ".ixx", ".mxx"];
	private static readonly string[] headerExts = [".h", ".hpp", ".hxx", ".hh", ".inl"];
	private static readonly Dictionary<string, string> depenencies = new();
	private static readonly Dictionary<File, List<string>> requires = new();
	private static readonly Dictionary<File, List<string>> includes = new();
	public static File ClCompile(this ITaskBuilder builder, File inFile, File objFile, Action<IClConfigurator> configurator)
	{
		var key = $"cl:{inFile.Path}";
		var depsKey = $"deps:{inFile.Path}";
		requires[inFile] = new();
		includes[inFile] = new();
		builder.buildAction(depsKey, $"Scan Dependencies: {inFile.Path}", () => [], async ctx =>
		{
			var cpp = ctx.getTool<ICppTool>();
			var deps = cpp.scanDependencies(inFile, objFile);
			configurator(deps);
			var result = await deps.run(ctx);
			lock (depenencies)
			{
				foreach (var d in deps.ProvidedDeps)
				{
					depenencies[d.Key] = d.Value;
				}
			}
			lock (includes)
			{
				includes[inFile].AddRange(deps.Includes);
			}
			lock (requires)
			{
				requires[inFile].AddRange(deps.Dependencies);
			}
			return result;
		}).buildAction(key, inFile.Path, () =>
		{
			lock (requires)
			{
				return requires[inFile].Select(r =>
				{
					lock (depenencies)
					{
						return depenencies.TryGetValue(r, out var val) ? val : Guid.NewGuid().ToString();
					}
				}).Select(r => $"cl:{r}").Prepend(depsKey);
			}
		}, async ctx =>
		{
			var cpp = ctx.getTool<ICppTool>();
			var config = cpp.compile(inFile, objFile);
			configurator(config);
			config.debugInformationFormat(DebugInformationFormat.EXTERNAL);
			var modules = requires[inFile].Select(r => depenencies[r]).ToArray();
			var dpsFile = Path.ChangeExtension(objFile.Path, ".dps");
			var deps = await Dependencies.FromFile(dpsFile);
			var newDeps = await Dependencies.FromCpp(inFile, includes[inFile].ToArray(),
				modules, config.Args);
			var needsRebuild = objFile.Exists == false || modules.Prepend(inFile.Path).Concat(includes[inFile])
				.Any(m => System.IO.File.GetLastWriteTimeUtc(m) > objFile.TimeStamp) || deps.needRebuild(newDeps);
			if (needsRebuild)
			{
				await newDeps.toFile(dpsFile);
				return await config.run(ctx);
			}
			return ActionResult.SKIP;
		});
		return objFile;
	}
	public static File ClCompile(this ITaskBuilder builder, File inFile, File objFile)
	{
		return builder.ClCompile(inFile, objFile, ctx => { });
	}

	public static IProjectBuilder Sources(this IProjectBuilder builder, string sourceDir,
		Action<IClConfigurator> configurator)
	{
		var files = cppExts.Concat(headerExts).Select(ext => "*" + ext).Select(m => builder.files(sourceDir, m)).SelectMany(f => f).ToArray();
		foreach (var file in files)
		{
			builder.makeSourceFile(file);
			if (file.IsCpp())
			{
				var objFile = builder.intermediateFile(file).changeExtension(FileType.OBJECT);
				builder.dependencies(deps => builder.task("build",
						tbuilder =>
						{
							tbuilder.ClCompile(file, objFile, cl =>
							{
								var depsFiles = deps.SelectMany(d => d.Key.SourceFiles.Select(f => new KeyValuePair<IProjectBuilder, File>(d.Key, f)))
									.Concat(files.Select(f => new KeyValuePair<IProjectBuilder, File>(builder, f)));
								foreach (var dep in depsFiles)
								{
									if (dep.Value.IsHeader())
									{
										cl.includeDir(Path.GetDirectoryName(dep.Value.Path)!);
									}
									else if (dep.Value.IsModule())
									{
										var ifcFile = dep.Key.intermediateFile(dep.Value).changeExtension(FileType.MODULE_INTERFACE);
										cl.module(ifcFile.Path);
									}
								}

								foreach (var define in builder.Defines().Concat(deps.SelectMany(d => d.Key.Defines())))
								{
									cl.macro(define.Key, define.Value);
								}
								configurator(cl);
							});
						}))
					.task("clean", tbuild => tbuild.buildAction($"clean:{objFile.Path}", $"Delete: {objFile.Path}", () => [],
						ctx =>
						{
							var dps = Path.ChangeExtension(objFile.Path, ".dps");
							var result = ActionResult.SKIP;
							if (System.IO.File.Exists(dps))
							{
								System.IO.File.Delete(dps);
								result = ActionResult.SUCCESS;
							}
							if (System.IO.File.Exists(objFile.Path))
							{
								System.IO.File.Delete(objFile.Path);
								result = ActionResult.SUCCESS;
							}
							return Task.FromResult(result);
						}));
			}
		}
		return builder;
	}
	public static IProjectBuilder Sources(this IProjectBuilder builder, string sourceDir)
	{
		return builder.Sources(sourceDir, _ => { });
	}
	public static IProjectBuilder Sources(this IProjectBuilder builder, Action<IClConfigurator> configurator)
	{
		return builder.Sources("src", configurator);
	}
	public static IProjectBuilder Sources(this IProjectBuilder builder)
	{
		return builder.Sources("src");
	}

	public static bool IsCpp(this File file)
	{
		var ext = Path.GetExtension(file.Path).ToLowerInvariant();
		return cppExts.Contains(ext);
	}
	public static bool IsHeader(this File file)
	{
		var ext = Path.GetExtension(file.Path).ToLowerInvariant();
		return headerExts.Contains(ext);
	}
	public static bool IsModule(this File file)
	{
		var ext = Path.GetExtension(file.Path).ToLowerInvariant();
		return ext == ".ixx" || ext == ".mxx";
	}
	public static IReadOnlyDictionary<string, string?> Defines(this IProjectBuilder builder)
	{
		return _defines.TryGetValue(builder.Name, out var defs) ? defs : new Dictionary<string, string?>();
	}
	public static IProjectBuilder Define(this IProjectBuilder builder, string name, string? value = null)
	{
		if (_defines.ContainsKey(builder.Name) == false)
		{
			_defines[builder.Name] = new();
		}
		_defines[builder.Name][name] = value;
		return builder;
	}
}