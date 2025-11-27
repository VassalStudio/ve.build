using System.Runtime.CompilerServices;
using ve.build.core;
using ve.build.core.projects;

namespace ve.build.projectgenerator;

public static class ProjectGeneratorExtension
{
	private static IProjectGenerator[] _generators = [];
	private static IProjectGenerator? _selectedGenerator = null;
	public static HostBuilder setupProjectGenerator(this HostBuilder builder, IProjectGenerator generator)
	{
		if (_generators.Length == 0)
		{
			builder.makeParam<IProjectGenerator?>("generator", null,
				(_, generator) => _selectedGenerator = generator ?? _generators.Last(), s => _generators.First(g => g.Name == s),
				"Specifies which project generator to use.").task("generateProjectFiles", "Generates project files for the selected generator.", taskBuilder => 
					generator.finalStep(taskBuilder.eachProject(projectBuilder => projectBuilder.sources(files => projectBuilder.dependencies(deps =>
						projectBuilder.task("generateProjectFiles", taskBuilder =>
						{
							var file = _selectedGenerator!.projectFile(projectBuilder);
							generator.setupProject(file);
							taskBuilder.buildAction($"gpf:{projectBuilder.Name}",
								$"Generates {file}",
								() => projectBuilder.Dependencies.Select(d => $"gpf:{d}"),
								ctx => _selectedGenerator!.generateProjectFiles(ctx, projectBuilder, files, deps.Keys));
						})))))
			);
		}
		_generators = _generators.Append(generator).ToArray();
		return builder;
	}
}