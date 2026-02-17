using ve.build.core.tasks;

namespace ve.build.core.projects;

internal class ProjectDescription
{
	public string Name { get; }
	public Action<IProjectBuilder>[] Builders { get; set; } = [];

	public string Output { get; }
	public string Intermediate { get; }
	public string Path { get; }
	public ProjectDescription(string name, Action<IProjectBuilder> builder, string path)
	{
		this.Name = name;
		this.Builders = [builder];
		this.Output = System.IO.Path.Join(path, "bin");
		this.Intermediate = System.IO.Path.Join(path, "obj");
		this.Path = path;
	}

	public KeyValuePair<Project, Dictionary<string, bool>> buildProject(List<Action<IProjectBuilder>> actions,
		ITaskBuilder taskBuilder, Func<FileType, string> getExtFunc)
	{
		var projectBuilder = new ProjectBuilder(taskBuilder, this.Name, this.Path, this.Output, this.Intermediate, getExtFunc);
		foreach (var builder in this.Builders)
		{
			builder(projectBuilder);
		}
		foreach (var builder in actions)
		{
			builder(projectBuilder);
		}
		return projectBuilder.build();
	}
}