using System.Text.Json.Serialization;
using ve.build.core;
using ve.build.core.projects;
using ve.build.core.tasks;
using ve.build.cpp.cpp;
using File = ve.build.core.files.File;

namespace ve.build.link.link;
public enum ProjectType
{
	EXE,
	DLL,
	STATIC_LIB
}

internal struct Dependencies
{
	private static int Combine(int seed, int value)
	{
		unchecked
		{
			return seed ^ (value + (int)0x9e3779b9 + (seed << 6) + (seed >> 2));
		}
	}

	private static int GetStableHashCode(byte[] bytes)
	{
		unchecked
		{
			const int p = 16777619;
			int hash = (int)2166136261;

			foreach (byte c in bytes)
				hash = (hash ^ c) * p;

			return hash;
		}
	}

	private static async Task<int> GetHashCode(File file)
	{
		if (System.IO.File.Exists(file.Path) == false) return -1;
		using var stream = System.IO.File.OpenRead(file.Path);
		var buffer = new byte[1024 * sizeof(long)];
		int read = 0;
		int hash = 0;
		while ((read = await stream.ReadAsync(buffer)) > 0)
		{
			if (read < buffer.Length)
			{
				hash = Combine(hash, GetStableHashCode(buffer.Take(read).ToArray()));
			}
		}

		return hash;
	}

	public Dependencies()
	{
	}

	private Dependencies(int hash, string[] configArgs)
	{
		this.Hash = hash;
		this.CommandLineArgs = configArgs;
	}

	[JsonPropertyName("hash")] public Int32 Hash { get; set; } = -1;
	[JsonPropertyName("commandLineArgs")] public string[] CommandLineArgs { get; set; } = [];

	public static async Task<Dependencies> FromFile(string path)
	{
		if (System.IO.File.Exists(path) == false)
		{
			await new Dependencies().toFile(path);
		}

		return System.Text.Json.JsonSerializer.Deserialize<Dependencies>(await System.IO.File.ReadAllTextAsync(path));
	}

	public async Task toFile(string path)
	{
		await System.IO.File.WriteAllTextAsync(path, System.Text.Json.JsonSerializer.Serialize(this));
	}

	public static async Task<Dependencies> FromBinary(File outFile, File[] files, string[] configArgs)
	{
		var hash = await GetHashCode(outFile);
		foreach (var file in files)
		{
			hash = Combine(hash, await GetHashCode(file));
		}

		return new Dependencies(hash, configArgs);
	}

	public bool needsRebuild(Dependencies other)
	{
		return this.Hash != other.Hash || this.CommandLineArgs.Except(other.CommandLineArgs).Any();
	}
}

public static class LinkExtension
{
	private static Dictionary<IProjectBuilder, ProjectType> _projectTypes = new();
	public static ITaskBuilder Link(this ITaskBuilder taskBuilder, File outputFile, Action<ILinkConfigurator> configurator, File[] files)
	{
		var key = $"link:{outputFile.Path}";
		taskBuilder.buildAction(key, outputFile.Path, () => files.Select(f => f.SourceFile!.IsCpp() ? $"cl:{f.SourceFile!.Path}" : $"link:{f.SourceFile!.Path}"), buildContext =>
		{
			var link = buildContext.getTool<ILinkTool>();
			var config = link.link(outputFile, files);
			configurator(config);
			switch (buildContext.Configuration)
			{
				case Configuration.DEBUG:
					config.enableDebugInformation(DebugInformation.FULL);
					break;
				case Configuration.RELEASE:
					config.enableDebugInformation(DebugInformation.NONE);
					break;
			}
			return runConfig(config, outputFile, files, buildContext);
		});
		return taskBuilder;
	}
	public static ITaskBuilder Link(this ITaskBuilder taskBuilder, File outputFile, File[] files)
	{
		return taskBuilder.Link(outputFile, _ => { }, files);
	}
	public static ITaskBuilder Lib(this ITaskBuilder taskBuilder, File outputFile, Action<ILibConfigurator> configurator, File[] files)
	{
		var key = $"lib:{outputFile.Path}";
		taskBuilder.buildAction(key, outputFile.Path, () => files.Select(f => $"cl:{f.Path}").ToArray(), buildContext =>
		{
			var link = buildContext.getTool<ILinkTool>();
			var config = link.lib(outputFile, files);
			configurator(config);
			return runConfig(config, outputFile, files, buildContext);
		});
		return taskBuilder;
	}

	private static async Task<ActionResult> runConfig(ILibConfigurator config, File outFile, File[] files, IBuildContext ctx)
	{
		var dir = Path.GetDirectoryName(outFile.Path)!;
		if (Directory.Exists(dir) == false)
		{
			Directory.CreateDirectory(dir);
		}
		var depsPath = Path.ChangeExtension(outFile.Path, ".dps");
		var deps = await Dependencies.FromFile(depsPath);
		var newDeps = await Dependencies.FromBinary(outFile, files, config.Args);
		var needRebuild = outFile.Exists == false || files.Any(f => f.TimeStamp > outFile.TimeStamp) || newDeps.needsRebuild(deps);
		if (needRebuild)
		{
			await newDeps.toFile(depsPath);
			return await config.run(ctx);
		}
		return ActionResult.SKIP;
	}

	public static ITaskBuilder Lib(this ITaskBuilder taskBuilder, File outputFile, File[] files)
	{
		return taskBuilder.Lib(outputFile, _ => { }, files);
	}

	public static IProjectBuilder Type(this IProjectBuilder builder, ProjectType type, Action<ILibConfigurator> configurator, Action<ILinkConfigurator> linkConfigurator)
	{
		_projectTypes[builder] = type;
		var outputFile = getOutFile(builder);
		var copyToOutput = (ITaskBuilder tb) => tb.eachProject(p => p.dependencies(d =>
		{
			if (d.Select(d => d.Key.Name).Contains(builder.Name))
			{
				tb.copy(outputFile, getOutFile(p), () => [$"link:{outputFile.Path}"]);
			}
		}));
		return builder.task("build", tbuilder => {
			tbuilder.eachProject(dp =>
			{
				var target = dp.outputFile(builder.Name + ".").changeExtension(FileType.SHARED_LIBRARY);
				if (dp.Dependencies.Contains(builder.Name) && string.Equals(outputFile.Path, target.Path) == false)
				{
					tbuilder.copy(outputFile, target, () => [$"link:{outputFile.Path}"]);
				}
			});
		}).sources(files => builder.dependencies(deps => builder.task("build",
			tbuilder =>
			{
				files = files.Where(f => f.IsCpp())
					.Select(f => builder.intermediateFile(f).changeExtension(FileType.OBJECT)).Concat(deps.Select(d => getOutFile(d.Key).changeExtension(FileType.STATIC_LIBRARY, true))).ToArray();
				switch (type)
				{
					case ProjectType.STATIC_LIB:
						tbuilder.Lib(outputFile, configurator, files);
						break;
					case ProjectType.DLL:
						tbuilder.Link(outputFile, ctx => linkConfigurator(ctx.dynamicLibrary(true)), files);
						break;
					case ProjectType.EXE:
						tbuilder.Link(outputFile, linkConfigurator, files);
						break;
				}
			}))).task("clean", tbuilder => tbuilder.buildAction($"clean:{outputFile.Path}", $"Delete: {outputFile.Path}", () => [],
			ctx =>
			{
				var dps = Path.ChangeExtension(outputFile.Path, ".dps");
				var result = ActionResult.SKIP;
				if (System.IO.File.Exists(dps))
				{
					System.IO.File.Delete(dps);
					result = ActionResult.SUCCESS;
				}
				if (System.IO.File.Exists(outputFile.Path))
				{
					System.IO.File.Delete(outputFile.Path);
					result = ActionResult.SUCCESS;
				}
				return Task.FromResult(result);
			}));
	}

	private static File getOutFile(IProjectBuilder builder)
	{
		return builder.outputFile(builder.Name + ".").changeExtension(GetProjectType(builder) switch
		{
			ProjectType.DLL => FileType.SHARED_LIBRARY,
			ProjectType.STATIC_LIB => FileType.STATIC_LIBRARY,
			_ => FileType.EXECUTABLE
		});
	}

	public static IProjectBuilder Type(this IProjectBuilder builder, ProjectType type)
	{
		return builder.Type(type, _ => { }, _ => { });
	}

	public static ProjectType GetProjectType(this IProjectBuilder builder)
	{
		return _projectTypes.TryGetValue(builder, out var type) ? type : ProjectType.EXE;
	}
}