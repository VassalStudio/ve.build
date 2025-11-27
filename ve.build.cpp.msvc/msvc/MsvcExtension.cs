using System.Diagnostics;
using System.Runtime.InteropServices;
using ve.build.core;
using ve.build.core.platform;
using ve.build.core.projects;
using ve.build.cpp.cpp;
using File = System.IO.File;

namespace ve.build.cpp.msvc.msvc;

public static class MsvcExtension
{
	public static HostBuilder useMsvc(this HostBuilder builder, Action<IClConfigurator> clConfigurator)
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
			string? x86_x64 = MsvcExtension._getToolPath("Microsoft.VisualStudio.Component.VC.Tools.x86.x64");
			string? arm64 = MsvcExtension._getToolPath("Microsoft.VisualStudio.Component.VC.Tools.ARM64");
			string host = RuntimeInformation.OSArchitecture switch
			{
				Architecture.X64 => "Hostx64",
				Architecture.X86 => "Hostx86",
				Architecture.Arm64 => "HostARM64",
				_ => throw new ArgumentOutOfRangeException()
			};
			var files = (IPlatformBuilder build) =>
			{
				build.makeFileType(FileType.OBJECT, ".obj").makeFileType(FileType.MODULE_INTERFACE, ".ifc");
			};
			if (x86_x64 != null)
			{
				builder.platform("windows-x64", RuntimeInformation.OSArchitecture == Architecture.X64,
					wbuild => files(wbuild.makeTool<ICppTool>(new MsvcTool(Path.Join(x86_x64, "bin", host, "x64", "cl.exe"), clConfigurator))))
					.platform("windows-x86", RuntimeInformation.OSArchitecture == Architecture.X86,
						wbuild => files(wbuild.makeTool<ICppTool>(new MsvcTool(Path.Join(x86_x64, "bin", host, "x86", "cl.exe"), clConfigurator))))
					.platform("efi-x64", false,
						ebuild => files(ebuild.makeTool<ICppTool>(new MsvcTool(Path.Join(x86_x64, "bin", host, "x64", "cl.exe"), clConfigurator))))
					.platform("efi-x86", false,
						ebuild => files(ebuild.makeTool<ICppTool>(new MsvcTool(Path.Join(x86_x64, "bin", host, "x86", "cl.exe"), clConfigurator))));
			}
			if (arm64 != null)
			{
				builder.platform("windows-arm64", RuntimeInformation.OSArchitecture == Architecture.Arm64,
					wbuild => files(wbuild.makeTool<ICppTool>(new MsvcTool(Path.Join(arm64, "bin", host, "arm64", "cl.exe"), clConfigurator))))
					.platform("efi-arm64", false,
						ebuild => files(ebuild.makeTool<ICppTool>(new MsvcTool(Path.Join(arm64, "bin", host, "arm64", "cl.exe"), clConfigurator))));
			}
		}
		return builder;
	}
	public static HostBuilder useMsvc(this HostBuilder builder)
	{
		return builder.useMsvc(_ => { });
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