using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using ve.build.core;
using ve.build.core.platform;
using ve.build.core.projects;
using ve.build.link.link;

namespace ve.build.link.msvc.msvc;

public static class MsvcLinkerExtension
{
	public static HostBuilder useMsvcLink(this HostBuilder builder, Action<ILibConfigurator> libConfigurator, Action<ILinkConfigurator> linkConfigurator)
	{
		bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		if (isWindows && RuntimeInformation.OSArchitecture switch
		{
			Architecture.X64 => true,
			Architecture.X86 => true,
			Architecture.Arm64 => true,
			_ => false
		})
		{
			string? x86_x64 = MsvcLinkerExtension._getToolPath("Microsoft.VisualStudio.Component.VC.Tools.x86.x64");
			string? arm64 = MsvcLinkerExtension._getToolPath("Microsoft.VisualStudio.Component.VC.Tools.ARM64");
			string? winSDK = Registry.LocalMachine.OpenSubKey("SOFTWARE")?.OpenSubKey("WOW6432Node")?.OpenSubKey("Microsoft")?.OpenSubKey("Microsoft SDKs")?.OpenSubKey("Windows")?
				.OpenSubKey("v10.0")?.GetValue("InstallationFolder") as string;
			string? libPath = winSDK != null ? Directory.EnumerateDirectories(Path.Join(winSDK, "Lib")).OrderBy(d => new Version(Path.GetFileName(d))).Last() : null;
			string host = RuntimeInformation.OSArchitecture switch
			{
				Architecture.X64 => "Hostx64",
				Architecture.X86 => "Hostx86",
				Architecture.Arm64 => "HostARM64",
				_ => throw new ArgumentOutOfRangeException()
			};
			var libPaths = (ILibConfigurator ctx, string arch, Action<ILibConfigurator> configuration) =>
			{
				if (libPath != null)
				{
					ctx.extraFlag($"/LIBPATH:{Path.Join(libPath, "ucrt", arch)}")
						.extraFlag($"/LIBPATH:{Path.Join(libPath, "um", arch)}");
				}

				configuration(ctx);
			};
			var winFiles = (IPlatformBuilder build) =>
			{
				build.makeFileType(FileType.EXECUTABLE, ".exe")
					.makeFileType(FileType.OBJECT, ".obj")
					.makeFileType(FileType.SHARED_LIBRARY, ".dll")
					.makeFileType(FileType.STATIC_LIBRARY, ".lib")
					.makeFileType(FileType.DEBUG_SYMBOLS, ".pdb");
			};
			var uefiFiles = (IPlatformBuilder build) =>
			{
				build.makeFileType(FileType.EXECUTABLE, ".efi")
					.makeFileType(FileType.OBJECT, ".obj")
					.makeFileType(FileType.SHARED_LIBRARY, ".dll")
					.makeFileType(FileType.STATIC_LIBRARY, ".lib")
					.makeFileType(FileType.DEBUG_SYMBOLS, ".pdb");
			};
			if (x86_x64 != null)
			{
				builder.platform("windows-x64", RuntimeInformation.OSArchitecture == Architecture.X64,
						wbuild => winFiles(wbuild.makeTool<ILinkTool>(new MsvcLinkTool(Path.Join(x86_x64, "bin", host, "x64"),
							Path.Join(x86_x64, "lib", "x64"), "WINDOWS", ctx => libPaths(ctx, "x64", libConfigurator),
							ctx => linkConfigurator(ctx.extraFlag($"/MACHINE:x64"))))))
					.platform("windows-x86", RuntimeInformation.OSArchitecture == Architecture.X86,
						wbuild => winFiles(wbuild.makeTool<ILinkTool>(new MsvcLinkTool(Path.Join(x86_x64, "bin", host, "x86"),
							Path.Join(x86_x64, "lib", "x86"), "WINDOWS", ctx => libPaths(ctx, "x86", libConfigurator),
							ctx => linkConfigurator(ctx.extraFlag($"/MACHINE:x86"))))))
					.platform("efi-x64", false,
						wbuild => uefiFiles(wbuild.makeTool<ILinkTool>(new MsvcLinkTool(Path.Join(x86_x64, "bin", host, "x64"),
							Path.Join(x86_x64, "lib", "x64"), "EFI_APPLICATION", libConfigurator, ctx => linkConfigurator(ctx.extraFlag($"/MACHINE:x64"))))))
					.platform("efi-x86", false,
						wbuild => uefiFiles(wbuild.makeTool<ILinkTool>(new MsvcLinkTool(Path.Join(x86_x64, "bin", host, "x86"),
							Path.Join(x86_x64, "lib", "x86"), "EFI_APPLICATION", libConfigurator, ctx => linkConfigurator(ctx.extraFlag($"/MACHINE:x86"))))));
			}
			if (arm64 != null)
			{
				builder.platform("windows-arm64", RuntimeInformation.OSArchitecture == Architecture.Arm64,
					wbuild => winFiles(wbuild.makeTool<ILinkTool>(new MsvcLinkTool(Path.Join(arm64, "bin", host, "arm64"),
						Path.Join(x86_x64, "lib", "arm64"), "WINDOWS", ctx => libPaths(ctx, "arm64", libConfigurator),
						ctx => linkConfigurator(ctx.extraFlag($"/MACHINE:arm64"))))))
					.platform("efi-arm64", RuntimeInformation.OSArchitecture == Architecture.Arm64,
					wbuild => uefiFiles(wbuild.makeTool<ILinkTool>(new MsvcLinkTool(Path.Join(arm64, "bin", host, "arm64"),
						Path.Join(x86_x64, "lib", "arm64"), "EFI_APPLICATION", libConfigurator, ctx => linkConfigurator(ctx.extraFlag($"/MACHINE:arm64"))))));
			}
		}
		return builder;
	}
	public static HostBuilder useMsvcLink(this HostBuilder builder)
	{
		return builder.useMsvcLink(_ => { }, _ => { });
	}

	private static string? _getToolPath(string key)
	{
		string vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
		if (File.Exists(vswhere))
		{
			var pi = new ProcessStartInfo(vswhere,
				["-latest", "-products", "*", "-requires", key, "-property", "installationPath", "-prerelease"]);
			pi.RedirectStandardOutput = true;
			pi.UseShellExecute = false;
			pi.CreateNoWindow = true;
			using var process = Process.Start(pi);
			process!.WaitForExit();
			var output = process.StandardOutput.ReadToEnd().Trim();
			if (process.ExitCode == 0 && Directory.Exists(output) && string.IsNullOrWhiteSpace(output) == false)
			{
				return Directory.EnumerateDirectories(Path.Join(output, "VC", "Tools", "MSVC"))
					.OrderByDescending(s => new Version(Path.GetFileName(s))).FirstOrDefault();
			}
		}
		return null;
	}
}