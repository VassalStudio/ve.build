using System.Diagnostics;
using ve.build.core;
using ve.build.core.projects;
using ve.build.core.tasks;
using ve.build.cpp.cpp;
using File = ve.build.core.files.File;

namespace ve.build.cpp.msvc.msvc;

internal class BaseConfigurator : IClConfigurator
{
	private string[] _args = [];
	private static readonly string[] _moduleExtensions = [".ixx", ".mxx"];

	protected BaseConfigurator(string clPath, File file, File outFile, Action<IClConfigurator> configurator)
	{
		this.Path = clPath;
		this.File = file;
		this.ObjFile = outFile;
		this.optimization(OptimizationLevel.NONE).inlineLevel(InlineLevel.NONE).enableIntrinsic(true)
			.splitSections(true).securityCheckers(true).rtti(true).favorOptimization(FavorOptimization.NONE)
			.exceptionHandling(ExceptionHandling.BOTH).extraFlags("/EHr").floatModel(FloatModel.PRECISE)
			.floatExceptions(false).floatContract(false).fastTranscendentals(false).linkTimeCodeGeneration(false)
			.extraFlags("/volatile:iso").stackCheck(false).addressSanitizer(false).arch(SSEArch.NONE)
			.vectorLength(VectorLength.AUTO).languageStandard(
				System.IO.Path.GetExtension(file.Path) switch
				{
					".cpp" or ".cxx" or ".cc" or ".c++" or ".ixx" or ".mxx" => LanguageStandard.CppLatest,
					".c" => LanguageStandard.CLatest,
					_ => throw new ArgumentOutOfRangeException()
				}).constexprBacktrace().constexprDepth().constexprSteps()
			.debugInformationFormat(DebugInformationFormat.EXTERNAL).throwingNew(true).openMP(false)
			.extraFlags("/nologo")
			.extraFlags("/c").extraFlags(file.Path).extraFlags($"/Fo{outFile.Path}").extraFlags($"/Fd{System.IO.Path.ChangeExtension(outFile.Path, ".pdb")}").macro("dllexport", "__declspec(dllexport)");
		if (_moduleExtensions.Any(ext => ext == System.IO.Path.GetExtension(file.Path)))
		{
			this.extraFlags($"/ifcOutput{outFile.changeExtension(FileType.MODULE_INTERFACE).Path}");
		}
		configurator(this);
	}

	private IClConfigurator _selectArgs<T>(T value, Dictionary<T, string?> map) where T : notnull
	{
		if (map.TryGetValue(value, out var arg))
		{
			this.removeFlags(map.ContainsValue);
			if (arg != null)
			{
				this.extraFlags(arg);
			}
		}
		return this;
	}
	public File File { get; }
	public string Path { get; }
	public File ObjFile { get; }
	public string[] Args => this._args;

	public virtual async Task<ActionResult> run(IBuildContext ctx)
	{
		var pi = new ProcessStartInfo(this.Path, this._args);
		ctx.log(LogLevel.VERBOSE, "MSVC", this.Path + " " + string.Join(' ', this.Args));
		pi.RedirectStandardOutput = true;
		pi.RedirectStandardError = true;
		pi.UseShellExecute = false;
		pi.CreateNoWindow = true;
		using var process = Process.Start(pi);
		await process!.WaitForExitAsync();
		var output = await process.StandardOutput.ReadToEndAsync();
		var error = await process.StandardError.ReadToEndAsync();
		if (process.ExitCode != 0)
		{
			ctx.log(LogLevel.ERROR, "MSVC", "Compilation failed:");
			foreach (var s in error.Split("\n").Where(s => string.IsNullOrWhiteSpace(s) == false))
			{
				ctx.log(LogLevel.ERROR, "MSVC", s);
			}
		}
		foreach (var s in output.Split("\n").Where(s => string.IsNullOrWhiteSpace(s) == false))
		{
			if (s.Trim() == System.IO.Path.GetFileName(this.File.Path)) continue;
			ctx.log(s.Contains("fatal error") ? LogLevel.FATAL : (s.Contains("error") ? LogLevel.ERROR : (s.Contains("warning") ? LogLevel.WARN : LogLevel.INFO)), "MSVC", s);
		}
		return process.ExitCode == 0 ? ActionResult.SUCCESS : ActionResult.FAILURE;
	}

	public IClConfigurator module(string modulePath)
	{
		return this.extraFlags($"/reference{modulePath}");
	}

	public IClConfigurator optimization(OptimizationLevel level)
	{
		return this._selectArgs(level, new()
		{
			[OptimizationLevel.NONE] = "/Od",
			[OptimizationLevel.SIZE] = "/O1",
			[OptimizationLevel.SPEED] = "/O2",
			[OptimizationLevel.MAXIMUM] = "/Ox"
		});
	}

	public IClConfigurator inlineLevel(InlineLevel level)
	{
		return this._selectArgs(level, new()
		{
			[InlineLevel.NONE] = "/Ob0",
			[InlineLevel.DEFAULT] = "/Ob1",
			[InlineLevel.FORCE] = "/Ob2"
		});
	}

	public IClConfigurator enableIntrinsic(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/Oi",
			[false] = "/Oi-"
		});
	}

	public IClConfigurator extraFlags(string flag)
	{
		this._args = this._args.Append(flag).ToArray();
		return this;
	}

	public IClConfigurator removeFlags(string flag)
	{
		return this.removeFlags(s => string.Equals(s, flag));
	}

	public IClConfigurator removeFlags(Func<string, bool> action)
	{
		this._args = this._args.Where(s => action(s) == false).ToArray();
		return this;
	}

	public IClConfigurator splitSections(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/Gy",
			[false] = "/Gy-"
		});
	}

	public IClConfigurator securityCheckers(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/GS",
			[false] = "/GS-"
		});
	}

	public IClConfigurator rtti(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/GR",
			[false] = "/GR-"
		});
	}

	public IClConfigurator favorOptimization(FavorOptimization favor)
	{
		return this._selectArgs(favor, new()
		{
			[FavorOptimization.NONE] = null,
			[FavorOptimization.SPEED] = "/Ot",
			[FavorOptimization.SIZE] = "/Os"
		});
	}

	public IClConfigurator exceptionHandling(ExceptionHandling handling)
	{
		return this._selectArgs(handling, new()
		{
			[ExceptionHandling.NONE] = "/EHs-c-",
			[ExceptionHandling.STRUCTURED] = "/EHs",
			[ExceptionHandling.CXX] = "/EHsc",
			[ExceptionHandling.BOTH] = "/EHa"
		});
	}

	public IClConfigurator floatModel(FloatModel model)
	{
		return this._selectArgs(model, new()
		{
			[FloatModel.FAST] = "/fp:fast",
			[FloatModel.PRECISE] = "/fp:precise",
			[FloatModel.STRICT] = "/fp:strict"
		});
	}

	public IClConfigurator floatExceptions(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/fp:except",
			[false] = "/fp:except-"
		});
	}

	public IClConfigurator floatContract(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/fp:contract",
			[false] = null
		});
	}

	public IClConfigurator fastTranscendentals(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/Qfast_transcendentals",
			[false] = null
		});
	}

	public IClConfigurator linkTimeCodeGeneration(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/GL",
			[false] = "/GL-"
		});
	}

	public IClConfigurator stackCheck(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/RTCs",
			[false] = null
		});
	}

	public IClConfigurator addressSanitizer(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/fsanitize=address",
			[false] = null
		});
	}

	public IClConfigurator arch(SSEArch arch)
	{
		return this._selectArgs(arch, new()
		{
			[SSEArch.NONE] = null,
			[SSEArch.SSE] = "/arch:SSE",
			[SSEArch.SSE2] = "/arch:SSE2",
			[SSEArch.SSE42] = "/arch:AVX",
			[SSEArch.AVX] = "/arch:AVX",
			[SSEArch.AVX2] = "/arch:AVX2",
			[SSEArch.AVX512] = "/arch:AVX512",
			[SSEArch.AVX10X] = "/arch:AVX10"
		});
	}

	public IClConfigurator vectorLength(VectorLength length)
	{
		return this._selectArgs(length, new()
		{
			[VectorLength.AUTO] = null,
			[VectorLength.V256] = "/vlen=256",
			[VectorLength.V512] = "/vlen=512"
		});
	}

	public IClConfigurator macro(string name, string? value = null)
	{
		return string.IsNullOrWhiteSpace(value) ? this.extraFlags($"/D{name}") : this.extraFlags($"/D{name}={value}");
	}

	public IClConfigurator forceInclude(File file)
	{
		return this.extraFlags($"/FI{file.Path}");
	}

	public IClConfigurator removeMacro(string name)
	{
		return this.extraFlags($"/U{name}");
	}

	public IClConfigurator includeDir(string path)
	{
		return this.extraFlags($"/I{path}");
	}

	public IClConfigurator noSTDIncludes(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/x",
			[false] = null
		});
	}

	public IClConfigurator languageStandard(LanguageStandard standard)
	{
		this._selectArgs(standard, new()
		{
			[LanguageStandard.C11] = "/Zc:__STDC__",
			[LanguageStandard.C17] = "/Zc:__STDC__",
			[LanguageStandard.CLatest] = "/Zc:__STDC__",
			[LanguageStandard.Cpp14] = "/Zc:__cplusplus",
			[LanguageStandard.Cpp17] = "/Zc:__cplusplus",
			[LanguageStandard.Cpp20] = "/Zc:__cplusplus",
			[LanguageStandard.CppLatest] = "/Zc:__cplusplus"
		});
		this._selectArgs(standard, new()
		{
			[LanguageStandard.C11] = "/TC",
			[LanguageStandard.C17] = "/TC",
			[LanguageStandard.CLatest] = "/TC",
			[LanguageStandard.Cpp14] = "/TP",
			[LanguageStandard.Cpp17] = "/TP",
			[LanguageStandard.Cpp20] = "/TP",
			[LanguageStandard.CppLatest] = "/TP"
		});
		return this._selectArgs(standard, new()
		{
			[LanguageStandard.C11] = "/std:c11",
			[LanguageStandard.C17] = "/std:c17",
			[LanguageStandard.CLatest] = "/std:clatest",
			[LanguageStandard.Cpp14] = "/std:c++14",
			[LanguageStandard.Cpp17] = "/std:c++17",
			[LanguageStandard.Cpp20] = "/std:c++20",
			[LanguageStandard.CppLatest] = "/std:c++latest"
		});
	}

	public IClConfigurator constexprDepth(int depth = 512)
	{
		return this.removeFlags(s => s.StartsWith("/constexpr:depth")).extraFlags($"/constexpr:depth{depth}");
	}

	public IClConfigurator constexprBacktrace(int backtrace = 5)
	{
		return this.removeFlags(s => s.StartsWith("/constexpr:backtrace")).extraFlags($"/constexpr:backtrace{backtrace}");
	}

	public IClConfigurator constexprSteps(int steps = 1048576)
	{
		return this.removeFlags(s => s.StartsWith("/constexpr:steps")).extraFlags($"/constexpr:steps{steps}");
	}

	public IClConfigurator debugInformationFormat(DebugInformationFormat format)
	{
		return this._selectArgs(format, new()
		{
			[DebugInformationFormat.NONE] = null,
			[DebugInformationFormat.INTERNAL] = "/Z7",
			[DebugInformationFormat.EXTERNAL] = "/Zi"
		});
	}

	public IClConfigurator structPacking(int packing)
	{
		return this.removeFlags(s => s.StartsWith("/Zp")).extraFlags($"/Zp{packing}");
	}

	public IClConfigurator throwingNew(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/Zc:throwingNew",
			[false] = "/Zc:throwingNew-"
		});
	}

	public IClConfigurator openMP(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/openmp:llvm",
			[false] = "/openmp-"
		});
	}
}
internal class ClConfigurator : BaseConfigurator
{
	public ClConfigurator(string clPath, File file, File objFile, Action<IClConfigurator> configurator) : base(clPath, file, objFile, configurator)
	{
		if (Directory.Exists(System.IO.Path.GetDirectoryName(objFile.Path)) == false)
		{
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(objFile.Path)!);
		}
	}
}

internal class ShowDependenciesConfigurator : BaseConfigurator, IScanDependenciesConfigurator
{
	private readonly List<string> _dependencies = [];
	private readonly Dictionary<string, string> _providedDependencies = new();
	private readonly List<string> _includes = [];
	public ShowDependenciesConfigurator(string clPath, File file, File outFile, Action<IClConfigurator> configurator) : base(clPath, file, outFile, configurator)
	{
		this.extraFlags("/scanDependencies-").extraFlags("/showIncludes");
	}

	public string[] Dependencies => this._dependencies.ToArray();
	public string[] Includes => this._includes.ToArray();

	public IReadOnlyDictionary<string, string> ProvidedDeps => this._providedDependencies;

	public override async Task<ActionResult> run(IBuildContext ctx)
	{
		var pi = new ProcessStartInfo(this.Path, this.Args);
		ctx.log(LogLevel.VERBOSE, "MSVC", this.Path + " " + string.Join(' ', this.Args));
		pi.RedirectStandardOutput = true;
		pi.RedirectStandardError = true;
		pi.UseShellExecute = false;
		pi.CreateNoWindow = true;
		using var process = Process.Start(pi);
		await process!.WaitForExitAsync();
		var output = await process.StandardOutput.ReadToEndAsync();
		var error = await process.StandardError.ReadToEndAsync();
		if (process.ExitCode != 0)
		{
			ctx.log(LogLevel.ERROR, "MSVC", "Compilation failed:");
			foreach (var s in error.Split("\n").Where(s => string.IsNullOrWhiteSpace(s) == false))
			{
				if (s.Trim() == System.IO.Path.GetFileName(this.File.Path)) continue;
				ctx.log(LogLevel.ERROR, "MSVC", s);
			}
		}
		else
		{
			foreach (var s in error.Split("\n").Where(s => string.IsNullOrWhiteSpace(s) == false))
			{
				ctx.log(LogLevel.VERBOSE, "MSVC", s);
				if (s.Trim() == System.IO.Path.GetFileName(this.File.Path)) continue;
				if (s.StartsWith("Note: including file:"))
				{
					var dep = s.Replace("Note: including file:", "").Trim();
					ctx.log(LogLevel.DEBUG, "MSVC", $"Detected include file: {dep}");
					this._includes.Add(dep);
				}
			}
			foreach (var s in output.Split("\n").Where(s => string.IsNullOrWhiteSpace(s) == false))
			{
				ctx.log(LogLevel.VERBOSE, "MSVC", s);
			}
			var deps = Dependency.FromJson(output);
			foreach (var dep in deps.Rules.SelectMany(r => r.Requires).Select(r => r.LogicalName))
			{
				ctx.log(LogLevel.DEBUG, "MSVC", $"Detected dependency module: {dep}");
				this._dependencies.Add(dep);
			}
			foreach (var dep in deps.Rules.SelectMany(r => r.Provides).Select(r => r.LogicalName))
			{
				ctx.log(LogLevel.DEBUG, "MSVC", $"Provided dependency module: {dep} at path {this.File.Path}");
				this._providedDependencies.Add(dep, this.File.Path);
			}
		}
		return process.ExitCode == 0 ? ActionResult.SUCCESS : ActionResult.FAILURE;
	}
}