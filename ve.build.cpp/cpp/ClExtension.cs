using ve.build.core.projects;
using ve.build.core.tasks;
using File = ve.build.core.files.File;

namespace ve.build.cpp.cpp;

public static class ClExtension
{
	private static readonly string[] fileMasks = ["*.c", "*.cpp", "*.cxx", "*.cc", "*.c++", "*.ixx", "*.mxx"];
	public static File ClCompile(this ITaskBuilder builder, File inFile, File objFile, Action<IClConfigurator> configurator)
	{
		var key = $"cl:{objFile.Path}";
		builder.buildAction(key, inFile.Path, inFile.Dependencies, ctx =>
		{
			var cpp = ctx.getTool<ICppTool>();
			var config = cpp.compile(inFile, objFile);
			configurator(config);
			return config.run(ctx);
		});
		objFile.addDependencies(key);
		return objFile;
	}
	public static File ClCompile(this ITaskBuilder builder, File inFile, File objFile)
	{
		return builder.ClCompile(inFile, objFile, ctx => { });
	}

	public static IProjectBuilder Sources(this IProjectBuilder builder, string sourceDir,
		Action<IClConfigurator> configurator)
	{
		var files = fileMasks.Select(m => builder.files(sourceDir, m)).SelectMany(f => f).ToArray();
		foreach (var file in files)
		{
			builder.makeSourceFile(file);
			builder.task("build",
				tbuilder => tbuilder.ClCompile(file, builder.intermediateFile(file, FileType.OBJECT)));
		}
		return builder;
	}
	public static IProjectBuilder Sources(this IProjectBuilder builder, string sourceDir)
	{
		return builder.Sources(sourceDir, _ => { });
	}
	public static IProjectBuilder Sources(this IProjectBuilder builder, Action<IClConfigurator> configurator)
	{
		return builder.Sources("src", configurator);
	}
	public static IProjectBuilder Sources(this IProjectBuilder builder)
	{
		return builder.Sources("src");
	}
}