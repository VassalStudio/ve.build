using Microsoft.Win32;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using ve.build.core;
using ve.build.core.projects;
using ve.build.core.tasks;
using ve.build.cpp.cpp;
using ve.build.link.link;
using ve.build.projectgenerator;

using File = ve.build.core.files.File;

namespace ve.build.vcxprojgenerator;

internal abstract class VcxprojGenerator(string name, string tools, string toolset, string file) : IProjectGenerator
{
	public string Name => name;

	public bool IsNative => (vswhere("-latest", "-property", "installationVersion") ?? "0").Split('.').First() ==
	                        this.Tools.Split('.').First();
	public string Tools => tools;
	public string Toolset => toolset;
	protected readonly string file = file;
	protected readonly List<string> _projectFiles = new();

	private static string? vswhere(params string[] args)
	{
		string vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
		if (System.IO.File.Exists(vswhere))
		{
			var pi = new ProcessStartInfo(vswhere, args);
			pi.RedirectStandardOutput = true;
			pi.UseShellExecute = false;
			pi.CreateNoWindow = true;
			using var process = Process.Start(pi);
			process!.WaitForExit();
			var output = process.StandardOutput.ReadToEnd().Trim();
			if (process.ExitCode == 0 && string.IsNullOrWhiteSpace(output) == false)
			{
				return output;
			}
		}
		return null;
	}

	private static string makeCommandLine(string? csproj, string[] args)
	{
		var currentProcess = Environment.ProcessPath;
		if (string.IsNullOrWhiteSpace(currentProcess) == false)
		{
			if (Path.GetFileNameWithoutExtension(currentProcess).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
			{
				if (csproj == null)
				{
					throw new Exception("Cannot build without a csproj file when using dotnet");
				}
				args = args.Prepend("--").Prepend('"' + csproj + '"').Prepend("--project").Prepend("Release")
					.Prepend("-c").Prepend("run").ToArray();
			}
			return string.Join(' ', args.Prepend('"' + currentProcess + '"'));
		}
		throw new Exception("Cannot determine current process path");
	}
	public async Task<ActionResult> generateProjectFiles(IBuildContext ctx, IProjectBuilder projectBuilder, File[] files, IEnumerable<IProjectBuilder> dependencies)
	{
		string? winSdk = OperatingSystem.IsWindows() ? Registry.LocalMachine.OpenSubKey("SOFTWARE")?.OpenSubKey("WOW6432Node")?.OpenSubKey("Microsoft")?.OpenSubKey("Microsoft SDKs")?.OpenSubKey("Windows")?
			.OpenSubKey("v10.0")?.GetValue("InstallationFolder") as string : null;
		Version winSdkVersion = winSdk != null ? Directory.EnumerateDirectories(Path.Join(winSdk, "Lib")).Select(d => new Version(Path.GetFileName(d))).OrderBy(d => d)
			.LastOrDefault(new Version("10.0.19041.0")) : new Version("10.0.19041.0");
		var primaryCsproj = this._foundCsproj(this.file);
		var path = this.projectFile(projectBuilder);
		var filter = path + ".filters";
		var projectBuilders = dependencies as IProjectBuilder[] ?? dependencies.ToArray();
		var dirs = files.Concat(projectBuilders.SelectMany(d => d.SourceFiles))
			.Select(f => Path.GetDirectoryName(f.Path)!).ToHashSet();
		var defines = projectBuilders.SelectMany(d => d.Defines()).Select(d => d.Value != null ? d.Key + "=" + d.Value : d.Key).Prepend("dllexport=__declspec(dllexport)");
		XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
		var configurations = ctx.Configurations.SelectMany(c => ctx.Platforms.Select(p => new KeyValuePair<string, string>(c.ToString(), p.Name))).ToArray();
		var project = new XElement(ns + "Project",
			new XAttribute("DefaultTargets", "Build"),
			new XAttribute("ToolsVersion", this.Tools),
			new XElement(ns + "ItemGroup",
				new XAttribute("Label", "ProjectConfigurations"),
				configurations.Select(c => new XElement(ns + "ProjectConfiguration",
					new XAttribute("Include", $"{c.Key}|{c.Value}"),
					new XElement(ns + "Configuration", c.Key),
					new XElement(ns + "Platform", c.Value)
				))
			),
			new XElement(ns + "PropertyGroup",
				new XAttribute("Label", "Globals"),
				new XElement(ns + "VCProjectVersion", this.Tools),
				new XElement(ns + "Keyword", "MakeFileProj"),
				new XElement(ns + "ProjectGuid", $"{{{GenerateGuid(path)}}}"),
				new XElement(ns + "WindowsTargetPlatformVersion", winSdkVersion.ToString())
			),
			new XElement(ns + "Import", new XAttribute("Project", @"$(VCTargetsPath)\Microsoft.Cpp.Default.props")),
			new XElement(ns + "PropertyGroup",
				new XAttribute("Label", "Configuration"),
				new XElement(ns + "ConfigurationType", "Makefile"),
				new XElement(ns + "PlatformToolset", this.Toolset)
			),
			new XElement(ns + "Import", new XAttribute("Project", @"$(VCTargetsPath)\Microsoft.Cpp.props")),
			await Task.WhenAll(configurations.Select(async c => new XElement(ns + "PropertyGroup",
					new XAttribute("Condition", $"'$(Configuration)|$(Platform)'=='{c.Key}|{c.Value}'"),
					new XElement(ns + "NMakeBuildCommandLine", makeCommandLine(primaryCsproj, ["build", $"--config={c.Key}", $"--platform={c.Value}", $"--project={projectBuilder.Name}"])),
					new XElement(ns + "NMakeReBuildCommandLine", makeCommandLine(primaryCsproj, ["rebuild", $"--config={c.Key}", $"--platform={c.Value}", $"--project={projectBuilder.Name}"])),
					new XElement(ns + "NMakeCleanCommandLine", makeCommandLine(primaryCsproj, ["clean", $"--config={c.Key}", $"--platform={c.Value}", $"--project={projectBuilder.Name}"])),
					new XElement(ns + "NMakeIncludeSearchPath", string.Join(';', dirs)),
					new XElement(ns + "NMakePreprocessorDefinitions", "$(NMakePreprocessorDefinitions);" + string.Join(';', defines)),
					new XElement(ns + "NMakeForcedIncludes", await this.generateForceInclude(projectBuilder, projectBuilders, c)),
					new XElement(ns + "AdditionalOptions", "/std:c++latest"),
					new XElement(ns + "NMakeOutput", Path.Join(projectBuilder.Path, "bin", c.Value, c.Key, Path.ChangeExtension(projectBuilder.Name + ".",
							ctx.Platforms.First(p => p.Name == c.Value).getExtension(projectBuilder.GetProjectType() switch
							{
								ProjectType.EXE => FileType.EXECUTABLE,
								ProjectType.DLL => FileType.SHARED_LIBRARY,
								ProjectType.STATIC_LIB => FileType.STATIC_LIBRARY,
								_ => throw new Exception("")
							})))),
					new XElement(ns + "NMakeWorkingDirectory", projectBuilder.Path),
					new XElement(ns + "IntDir", $"{projectBuilder.Path}\\obj\\{c.Value}\\{c.Key}\\"),
					new XElement(ns + "OutDir", $"{projectBuilder.Path}\\bin\\{c.Value}\\{c.Key}\\"),
					new XElement(ns + "ExtensionsToDeleteOnClean", "*.cdf;*.cache;*.obj;*.ilk;*.resources;*.tlog;*.manifest;*.res;*.rc;*.pdb;*.exp;*.idb;*.rep;*.xdc;*.sbr;*.bsc"),
					new XElement(ns + "DebuggerFlavor", "WindowsLocalDebugger"),
					new XElement(ns + "LocalDebuggerDebuggerType", "NativeOnly"),
					new XElement(ns + "LocalDebuggerWorkingDirectory", projectBuilder.Path),
					new XElement(ns + "LocalDebuggerCommand", "$(NMakeOutput)")
			))),
			new XElement(ns + "ItemGroup", files.Select(f => new XElement(ns + (f.IsCpp() ?
					"ClCompile" : (f.IsHeader() ? "ClInclude" : "None")),
				new XAttribute("Include", f.Path)))),
			new XElement(ns + "ItemGroup", projectBuilders.Select(d => new XElement(ns + "ProjectReference",
					new XAttribute("Include", projectFile(d)),
					new XElement(ns + "Project", $"{{{GenerateGuid(projectFile(d))}}}")
				))),
			new XElement(ns + "Import", new XAttribute("Project", @"$(VCTargetsPath)\Microsoft.Cpp.targets"))
		);
		var doc = new XDocument(
			new XDeclaration("1.0", "utf-8", "yes"),
			project
		);
		await using var stream = System.IO.File.Create(path);
		await doc.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
		var filters = new XElement(ns + "Project",
			new XAttribute("ToolsVersion", "4.0"),
			new XElement(ns + "ItemGroup",
				files.Select(f => Path.GetDirectoryName(f.Path)).ToHashSet().Where(p => p != null).Select(p => Path.GetRelativePath(projectBuilder.Path, p!))
					.Select(p => new XElement(ns + "Filter",
						new XAttribute("Include", p),
						new XElement(ns + "UniqueIdentifier", GenerateGuid(p).ToString("B"))
					))
			),
			new XElement(ns + "ItemGroup",
				files.Select(f => new XElement(ns + (f.IsCpp() ?
						"ClCompile" : (f.IsHeader() ? "ClInclude" : "None")),
					new XAttribute("Include", f.Path),
					new XElement(ns + "Filter", Path.GetDirectoryName(Path.GetRelativePath(projectBuilder.Path, f.Path)))
				))
			)
		);
		var filtersDoc = new XDocument(
			new XDeclaration("1.0", "utf-8", "yes"),
			filters
		);
		await using var filterStream = System.IO.File.Create(filter);
		await filtersDoc.SaveAsync(filterStream, SaveOptions.None, CancellationToken.None);
		return ActionResult.SUCCESS;
	}

	private async Task<string> generateForceInclude(IProjectBuilder projectBuilder, IEnumerable<IProjectBuilder> dependencies, KeyValuePair<string, string> config)
	{
		var path = Path.Join(projectBuilder.Path, "obj", config.Value, config.Key);
		if (Directory.Exists(path) == false)
		{
			Directory.CreateDirectory(path);
		}
		path = Path.Join(path, $"{projectBuilder.Name}.h");
		await using var forceInclude =
			new StreamWriter(System.IO.File.Create(path));
		await forceInclude.WriteLineAsync("#pragma once");
		foreach (var def in projectBuilder.Defines().Append(new KeyValuePair<string, string?>("dllexport", "__declspec(dllexport)")))
		{
			await forceInclude.WriteLineAsync($"#define {def.Key} {def.Value}");
		}

		return string.Join(';', dependencies.Select(d => Path.Join(projectBuilder.Path, "obj", config.Key, config.Value, $"{d.Name}.h")).Append(path));
	}

	public static Guid GenerateGuid(string input)
	{
		using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
		{
			byte[] hash = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(input));
			return new Guid(hash);
		}
	}

	public string projectFile(IProjectBuilder projectBuilder)
	{
		return Path.Join(projectBuilder.Path, "obj", $"{projectBuilder.Name}.vcxproj");
	}

	public abstract void finalStep(ITaskBuilder taskBuilder);

	public void setupProject(string file)
	{
		if (this._projectFiles.Contains(file) == false)
		{
			this._projectFiles.Add(file);
		}
	}

	protected bool _hasCsproj(string sln, string csproj)
	{
		return this.csProjects(sln, null).FirstOrDefault(p => p == csproj) != null;
	}

	protected abstract IEnumerable<string> csProjects(string sln, string? primaryProject);

	protected string? _foundCsproj(string path)
	{
		while (string.IsNullOrWhiteSpace(path) == false)
		{
			var dir = Path.GetDirectoryName(path)!;
			var csproj = Directory.EnumerateFiles(dir, "*.csproj").FirstOrDefault();
			if (csproj != null)
			{
				return csproj;
			}
		}

		return null;
	}
}

internal class Vcxproj2022(string file) : VcxprojGenerator("vs2022", "17.0", "v143", file)
{
	private string? _foundSln(string path)
	{
		var csproj = this._foundCsproj(path) ?? Environment.ProcessPath;
		path = csproj;
		while (string.IsNullOrWhiteSpace(path) == false)
		{
			path = Path.GetDirectoryName(path)!;
			if (string.IsNullOrWhiteSpace(path)) break;
			var sln = Directory.EnumerateFiles(path, "*.sln").FirstOrDefault(sln => this._hasCsproj(sln, csproj));
			if (sln != null)
			{
				return sln;
			}
		}

		return null;
	}
	public override void finalStep(ITaskBuilder taskBuilder)
	{
		var primaryCsproj = this._foundCsproj(this.file);
		var sln = this._foundSln(this.file) ?? Path.ChangeExtension(primaryCsproj, ".sln");
		var currentProcess = Environment.ProcessPath;
		if (string.IsNullOrWhiteSpace(currentProcess) == false && primaryCsproj == null)
		{
			if (Path.GetFileNameWithoutExtension(currentProcess).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
			{
				throw new Exception("Cannot build without a csproj file when using dotnet");
			}
			sln = Path.ChangeExtension(this.file, ".sln");
		}
		var csprojects = this.csProjects(sln, primaryCsproj).ToArray();
		taskBuilder.buildAction($"gsf:{sln}", $"Generate {sln}", () => this._projectFiles.Select(p => $"gpf:{Path.GetFileNameWithoutExtension(p)}"),
			async ctx =>
			{
				var configurations = ctx.Configurations.SelectMany(c => ctx.Platforms.Select(p => new KeyValuePair<string, string>(c.ToString(), p.Name))).ToArray();
				using var writer = new StreamWriter(System.IO.File.Create(sln!));
				await writer.WriteLineAsync("Microsoft Visual Studio Solution File, Format Version 12.00");
				await writer.WriteLineAsync("# Visual Studio Version 17");
				await writer.WriteLineAsync("VisualStudioVersion = 17.0.31903.59");
				await writer.WriteLineAsync("MinimumVisualStudioVersion = 10.0.40219.1");
				foreach (var csproject in csprojects)
				{
					await writer.WriteLineAsync(
						$"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{Path.GetFileNameWithoutExtension(csproject)}\", \"{csproject}\", \"{{{GenerateGuid(csproject).ToString()}}}\"");
					await writer.WriteLineAsync("EndProject");
				}
				foreach (var vcxproj in this._projectFiles)
				{
					await writer.WriteLineAsync(
						$"Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"{Path.GetFileNameWithoutExtension(vcxproj)}\", \"{vcxproj}\", \"{{{GenerateGuid(vcxproj).ToString()}}}\"");
					await writer.WriteLineAsync("\tProjectSection(ProjectDependencies) = postProject");
					if (primaryCsproj != null)
					{
						await writer.WriteLineAsync($"\t\t{{{GenerateGuid(primaryCsproj)}}} = {{{GenerateGuid(primaryCsproj)}}}");
					}
					await writer.WriteLineAsync("\tEndProjectSection");
					await writer.WriteLineAsync("EndProject");
				}
				await writer.WriteLineAsync("Global");
				await writer.WriteLineAsync("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
				foreach (var config in configurations)
				{
					await writer.WriteLineAsync($"\t\t{config.Key}|{config.Value} = {config.Key}|{config.Value}");
				}
				await writer.WriteLineAsync("\tEndGlobalSection");
				await writer.WriteLineAsync("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
				foreach (var vcxproj in this._projectFiles)
				{
					foreach (var config in configurations)
					{
						await writer.WriteLineAsync($"\t\t{{{GenerateGuid(vcxproj)}}}.{config.Key}|{config.Value}.ActiveCfg = {config.Key}|{config.Value}");
						await writer.WriteLineAsync($"\t\t{{{GenerateGuid(vcxproj)}}}.{config.Key}|{config.Value}.Build.0 = {config.Key}|{config.Value}");
					}
				}
				foreach (var csproject in csprojects)
				{
					foreach (var config in configurations)
					{
						await writer.WriteLineAsync($"\t\t{{{GenerateGuid(csproject)}}}.{config.Key}|{config.Value}.ActiveCfg = Release|Any CPU");
						await writer.WriteLineAsync($"\t\t{{{GenerateGuid(csproject)}}}.{config.Key}|{config.Value}.Build.0 = Release|Any CPU");
					}
				}
				await writer.WriteLineAsync("\tEndGlobalSection");
				await writer.WriteLineAsync("\tGlobalSection(SolutionProperties) = preSolution");
				await writer.WriteLineAsync("\t\tHideSolutionNode = FALSE");
				await writer.WriteLineAsync("\tEndGlobalSection");
				await writer.WriteLineAsync("EndGlobal");
				return ActionResult.SUCCESS;
			});
	}

	protected override IEnumerable<string> csProjects(string sln, string? primaryProject)
	{
		if (System.IO.File.Exists(sln) == false) return primaryProject != null ? [primaryProject] : [];
		var dir = Path.GetDirectoryName(sln)!;
		return System.IO.File.ReadAllLines(sln)
			.Where(l => l.Trim().StartsWith("Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = "))
			.Select(l => l.Split(',').Take(new Range(1, 2)).First())
			.Select(l => l.Trim().Trim('"')).Select(p => Path.IsPathFullyQualified(p) ? p : Path.Join(dir, p));
	}
}
internal class Vcxproj2026(string file) : VcxprojGenerator("vs2026", "18.0", "v144", file)
{
	private string? _foundSln(string path)
	{
		var csproj = this._foundCsproj(path) ?? Environment.ProcessPath;
		path = csproj;
		while (string.IsNullOrWhiteSpace(path) == false)
		{
			path = Path.GetDirectoryName(path)!;
			if (string.IsNullOrWhiteSpace(path)) break;
			var sln = Directory.EnumerateFiles(path, "*.slnx").FirstOrDefault(sln => this._hasCsproj(sln, csproj));
			if (sln != null)
			{
				return sln;
			}
		}
		return null;
	}
	public override void finalStep(ITaskBuilder taskBuilder)
	{
		var primaryCsproj = this._foundCsproj(this.file);
		var sln = this._foundSln(this.file) ?? Path.ChangeExtension(primaryCsproj, ".slnx");
		var currentProcess = Environment.ProcessPath;
		if (string.IsNullOrWhiteSpace(currentProcess) == false && primaryCsproj == null)
		{
			if (Path.GetFileNameWithoutExtension(currentProcess).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
			{
				throw new Exception("Cannot build without a csproj file when using dotnet");
			}
			sln = Path.ChangeExtension(this.file, ".slnx");
		}
		var csprojects = this.csProjects(sln!, primaryCsproj).ToArray();
		taskBuilder.buildAction($"gsf:{sln}", $"Generate {sln}", () => this._projectFiles.Select(p => $"gpf:{Path.GetFileNameWithoutExtension(p)}"),
			async ctx =>
			{
				await using var filterStream = System.IO.File.Create(sln!);
				await new XDocument(
					new XElement("Solution",
						new XElement("Configurations",
							ctx.Configurations.Select(c => new XElement("BuildType", new XAttribute("Name", c.ToString()))),
							ctx.Platforms.Select(p => new XElement("Platform", new XAttribute("Name", p.Name)))),
						csprojects.Select(csproj => new XElement("Project",
							new XAttribute("Path", csproj),
							new XElement("BuildType", new XAttribute("Project", "Release")))),
						this._projectFiles.Select(vcxproj => new XElement("Project",
							new XAttribute("Path", vcxproj),
							new XAttribute("Id", GenerateGuid(vcxproj)),
							primaryCsproj != null ? new XElement("BuildDependency",
								new XAttribute("Project", primaryCsproj)) : null)))
					).SaveAsync(filterStream, SaveOptions.None, CancellationToken.None);
				return ActionResult.SUCCESS;
			});
	}

	protected override IEnumerable<string> csProjects(string sln, string? primaryProject)
	{
		if (System.IO.File.Exists(sln) == false) return primaryProject != null ? [primaryProject] : [];
		using var steam = System.IO.File.OpenRead(sln);
		try
		{
			List<string> projects = new();
			var slnDoc = XDocument.Load(steam);
			this._scanProjectsRecurse(slnDoc, projects);
			return projects;
		}
		catch
		{
			return primaryProject != null ? [primaryProject] : [];
		}
	}

	private void _scanProjectsRecurse(XContainer slnDoc, List<string> projects)
	{
		foreach (var csproj in slnDoc.Elements("Project").Select(p => p.Attribute("Path")).Where(a => a != null).Select(a => a!.Value).Where(p => Path.GetExtension(p) == ".csproj"))
		{
			projects.Add(csproj);
		}

		foreach (var element in slnDoc.Elements())
		{
			this._scanProjectsRecurse(element, projects);
		}
	}
}
public static class VcxprojGeneratorExtension
{
	public static HostBuilder useVcxprojGenerator(this HostBuilder builder, [CallerFilePath] string? file = null)
	{
		return builder.setupProjectGenerator(new Vcxproj2022(file!)).setupProjectGenerator(new Vcxproj2026(file!));
	}
}