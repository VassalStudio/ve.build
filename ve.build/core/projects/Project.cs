using ve.build.core.buildgraph;

namespace ve.build.core.projects;

internal class Project
{
	private readonly ProjectBuilder _builder;

	public Project(string name, string path, string output, string intermediate, ProjectBuilder builder)
	{
		this.Name = name;
		this.Output = output;
		this.Intermediate = intermediate;
		this.Path = path;
		this._builder = builder;
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
		this._addrecursiveDependencies(this.Dependencies, this.Dependencies);
		this.PrivateDependencies.AddRange(projects.Where(p => dependencies.TryGetValue(p.Name, out bool publicDependency) && publicDependency == false));
		this._addrecursiveDependencies(this.PrivateDependencies, this.PrivateDependencies);
		var publicDependencies = this.Dependencies.Select(p => p._builder).ToArray();
		var privateDependencies = this.PrivateDependencies.Select(p => p._builder).ToArray();
		var deps = publicDependencies.Select(d => new KeyValuePair<IProjectBuilder, bool>(d, true))
			.Concat(privateDependencies.Select(d => new KeyValuePair<IProjectBuilder, bool>(d, false))).ToDictionary();
		foreach (var builder in this._builder.DependencyCallbacks)
		{
			builder(deps);
		}

		foreach (var callback in this._builder.SourceCallbacks)
		{
			callback(this._builder.SourceFiles);
		}
		return this;
	}

	private void _addrecursiveDependencies(List<Project> dependencies, List<Project> target)
	{
		foreach (var dep in dependencies.ToArray())
		{
			target.AddRange(dep.Dependencies);
			this._addrecursiveDependencies(dep.Dependencies, target);
		}
	}
}