using ve.build.core.buildgraph;

namespace ve.build.core.projects;

internal class Project
{
	public Project(string name, string path, string output, string intermediate)
	{
		this.Name = name;
		this.Output = output;
		this.Intermediate = intermediate;
		this.Path = path;
	}

	public string Name { get; }
	public string Intermediate { get; }
	public string Output { get; }
	public string Path { get; }

	public List<Project> Dependencies { get; } = new();
	public List<Project> PrivateDependencies { get; } = new();

	public Project resolveDependencies(Project[] projects, Dictionary<string, bool> dependencies)
	{
		this.Dependencies.AddRange(projects.Where(p => dependencies.TryGetValue(p.Name, out bool publicDependency) && publicDependency));
		this.PrivateDependencies.AddRange(projects.Where(p => dependencies.TryGetValue(p.Name, out bool publicDependency) && publicDependency == false));
		return this;
	}
}