using ve.build.core;
using ve.build.core.projects;

await new HostBuilder().task("build", "Build sample task",
	builder => builder.eachProject(pbuilder => pbuilder.buildAction("defaultAction", "Default Action", [], ctx => ctx.log(LogLevel.INFO, "BUILD", "Test Build"))))
	.project("sample", PROJECT_TYPE.APPLICATION).build().run(args);