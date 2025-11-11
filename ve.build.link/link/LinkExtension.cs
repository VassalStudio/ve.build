using ve.build.core.projects;
using ve.build.core.tasks;

using File = ve.build.core.files.File;

namespace ve.build.link.link;
public enum ProjectType
{
	EXE,
	DLL,
	STATIC_LIB
}

public static class LinkExtension
{
	public static ITaskBuilder Link(this ITaskBuilder taskBuilder, File outputFile, Action<ILinkConfigurator> configurator, File[] files)
	{
		var key = $"link:{outputFile.Path}";
		taskBuilder.buildAction(key, outputFile.Path, files.SelectMany(f => f.Dependencies).ToArray(), buildContext =>
		{
			var link = buildContext.getTool<ILinkTool>();
			var config = link.link(outputFile, files);
			configurator(config);
			return config.run(buildContext);
		});
		outputFile.addDependencies(key);
		return taskBuilder;
	}
	public static ITaskBuilder Link(this ITaskBuilder taskBuilder, File outputFile, File[] files)
	{
		return taskBuilder.Link(outputFile, _ => { }, files);
	}
	public static ITaskBuilder Lib(this ITaskBuilder taskBuilder, File outputFile, Action<ILibConfigurator> configurator, File[] files)
	{
		var key = $"lib:{outputFile.Path}";
		taskBuilder.buildAction(key, outputFile.Path, files.SelectMany(f => f.Dependencies).ToArray(), buildContext =>
		{
			var link = buildContext.getTool<ILinkTool>();
			var config = link.lib(outputFile, files);
			configurator(config);
			return config.run(buildContext);
		});
		outputFile.addDependencies(key);
		return taskBuilder;
	}
	public static ITaskBuilder Lib(this ITaskBuilder taskBuilder, File outputFile, File[] files)
	{
		return taskBuilder.Lib(outputFile, _ => { }, files);
	}

	public static IProjectBuilder Type(this IProjectBuilder builder, ProjectType type, Action<ILibConfigurator> configurator, Action<ILinkConfigurator> linkConfigurator)
	{
		var outputFile = builder.outputFile(builder.Name + ".", type switch
		{
			ProjectType.DLL => FileType.SHARED_LIBRARY,
			ProjectType.STATIC_LIB => FileType.STATIC_LIBRARY,
			_ => FileType.EXECUTABLE
		});
		
		return builder.sources(files => builder.task("build",
			tbuilder =>
			{
				files = files.Select(f => builder.intermediateFile(f, FileType.OBJECT)).ToArray();
				switch (type)
				{
					case ProjectType.STATIC_LIB:
						tbuilder.Lib(outputFile, configurator, files);
						break;
					case ProjectType.DLL:
						tbuilder.Link(outputFile, ctx => linkConfigurator(ctx.dynamicLibrary(true)), files);
						break;
					case ProjectType.EXE:
						tbuilder.Link(outputFile, linkConfigurator, files);
						break;
				}
			}));
	}
	public static IProjectBuilder Type(this IProjectBuilder builder, ProjectType type)
	{
		return builder.Type(type, _ => { }, _ => { });
	}
}