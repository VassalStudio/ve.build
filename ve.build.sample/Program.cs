using ve.build.core;
using ve.build.cpp.cpp;
using ve.build.link.link;
using ve.build.msvc;

await new HostBuilder()
	.project("sample", pbuilder => pbuilder.Type(ProjectType.EXE).Sources())
	.useMsvcToolchain().build().run(args);