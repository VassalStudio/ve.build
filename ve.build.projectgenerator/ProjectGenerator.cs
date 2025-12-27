using ve.build.core;
using ve.build.core.projects;
using ve.build.core.tasks;
using File = ve.build.core.files.File;

namespace ve.build.projectgenerator;

public interface IProjectGenerator
{
	string Name { get; }
	bool IsNative { get; }
	Task<ActionResult> generateProjectFiles(IBuildContext ctx, IProjectBuilder projectBuilder, File[] files, IEnumerable<IProjectBuilder> dependencies);
	string projectFile(IProjectBuilder projectBuilder);
	void finalStep(ITaskBuilder taskBuilder);
	void setupProject(string file);
}