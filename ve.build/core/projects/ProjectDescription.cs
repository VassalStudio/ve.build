namespace ve.build.core.projects;

internal class ProjectDescription
{
	public string Name { get; }
	public PROJECT_TYPE Type { get; }
	public Action<IProjectBuilder>[] Builders { get; set; } = [];
	public ProjectDescription(string name, PROJECT_TYPE type, Action<IProjectBuilder> builder)
	{
		this.Name = name;
		this.Type = type;
		this.Builders = [builder];
	}

	public KeyValuePair<Project, Dictionary<string, bool>> buildProject(Action<IProjectBuilder>[] actions)
	{
		var projectBuilder = new ProjectBuilder(this.Name, this.Type);
		foreach (var builder in this.Builders.Concat(actions))
		{
			builder(projectBuilder);
		}
		return projectBuilder.build();
	}
}