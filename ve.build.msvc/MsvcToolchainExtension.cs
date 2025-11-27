using System.Runtime.CompilerServices;
using ve.build.core;
using ve.build.cpp.cpp;
using ve.build.cpp.msvc.msvc;
using ve.build.link.link;
using ve.build.link.msvc.msvc;
using ve.build.vcxprojgenerator;

namespace ve.build.msvc;

public static class MsvcToolchainExtension
{
	public static HostBuilder useMsvcToolchain(this HostBuilder builder, Action<IClConfigurator> clConfigurator, Action<ILibConfigurator> libConfigurator, Action<ILinkConfigurator> linkConfigurator, [CallerFilePath] string? file = null)
	{
		return builder.useMsvc(clConfigurator).useMsvcLink(libConfigurator, linkConfigurator).useVcxprojGenerator(file);
	}
	public static HostBuilder useMsvcToolchain(this HostBuilder builder, [CallerFilePath]string? file = null)
	{
		return builder.useMsvcToolchain(_ => { }, _ => { }, _ => { }, file);
	}
}