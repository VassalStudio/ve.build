using ve.build.core;
using ve.build.cpp.cpp;
using ve.build.link.link;
using ve.build.msvc;

return await new HostBuilder()
	.project("sample.lib", pbuilder => pbuilder.Type(ProjectType.DLL).Sources("lib"))
	.project("sample", pbuilder => pbuilder.Type(ProjectType.EXE).Sources().dependsOf("sample.lib"))
	.useMsvcToolchain().build().run(args);