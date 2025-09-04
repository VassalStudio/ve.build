using ve.build.core.buildgraph;

namespace ve.build.core.projects;

internal class Project
{
	public Project(string name, PROJECT_TYPE type)
	{
		this.Name = name;
		this.Type = type;
	}

	public string Name { get; }

	public PROJECT_TYPE Type { get; }

	public List<Project> Dependencies { get; } = new();
	public List<Project> PrivateDependencies { get; } = new();
	public readonly List<DagNode> BuildNode = new();

	public Project resolveDependencies(Project[] projects, Dictionary<string, bool> dependencies)
	{
		this.Dependencies.AddRange(projects.Where(p => dependencies.TryGetValue(p.Name, out bool publicDependency) && publicDependency));
		this.PrivateDependencies.AddRange(projects.Where(p => dependencies.TryGetValue(p.Name, out bool publicDependency) && publicDependency == false));
		return this;
	}

	public void makeBuildNode(string key, string name, string[] dependencies, Func<IBuildContext, Task> buildAction)
	{
		this.BuildNode.Add(new DagNode(key, name, dependencies, buildAction));
	}
}