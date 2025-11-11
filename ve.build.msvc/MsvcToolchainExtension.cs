using ve.build.core;
using ve.build.cpp.cpp;
using ve.build.cpp.msvc.msvc;
using ve.build.link.link;
using ve.build.link.msvc.msvc;

namespace ve.build.msvc;

public static class MsvcToolchainExtension
{
	public static HostBuilder useMsvcToolchain(this HostBuilder builder, Action<IClConfigurator> clConfigurator, Action<ILibConfigurator> libConfigurator, Action<ILinkConfigurator> linkConfigurator)
	{
		return builder.useMsvc(clConfigurator).useMsvcLink(libConfigurator, linkConfigurator);
	}
	public static HostBuilder useMsvcToolchain(this HostBuilder builder)
	{
		return builder.useMsvcToolchain(_ => { }, _ => { }, _ => { });
	}
}