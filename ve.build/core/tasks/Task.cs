using ve.build.core.buildgraph;
using ve.build.core.projects;

namespace ve.build.core.tasks;

internal class Task
{
	private Task[] _dependencies;
	public readonly Dictionary<string, string> _paramHelps = new();
	public readonly List<DagNode> BuildNodes = new();
	private readonly ITaskBuilder _builder;

	public Task(string name, string description, ITaskBuilder builder)
	{
		this._builder = builder;
		this.Name = name;
		this.Description = description;
		this._dependencies = Array.Empty<Task>();
		this.ProjectActions = Array.Empty<Action<IProjectBuilder>>();
	}

	public Action<IProjectBuilder>[] ProjectActions { get; set; }


	public Task resolveDependencies(Task[] tasks)
	{
		this._dependencies = tasks;
		foreach (var node in this.BuildNodes)
		{
			foreach (var task in tasks)
			{
				node.makeDependencies(() => task.BuildNodes.Select(n => n.Key).ToArray());
			}
		}
		return this;
	}

	public string Name { get; }
	public string Description { get; }
	public bool ShouldPrintHelp { get; set; }

	public Dag buildGraph(List<ProjectDescription> projectsDescs, Func<FileType, string> getExtFunc, ProjectDescription? selectedProject)
	{
		Dictionary<Project, Dictionary<string, bool>> projects = new();
		ProjectDescription[] projectsArray = selectedProject != null ? [selectedProject] : projectsDescs.ToArray();
		do
		{
			var built = this.buildProject(projectsArray, getExtFunc);
			projects = projects.Concat(built).ToDictionary();
			projectsArray = projectsDescs.Where(p => built.Values.SelectMany(v => v.Keys).Contains(p.Name)
			                                         && projects.Select(p => p.Key.Name).Contains(p.Name) == false).ToArray();
		} while (projectsArray.Length > 0);

		List<Project> resolved = new();
		while (resolved.Count < projects.Count)
		{
			var pair = projects.First(p => p.Value.All(d => resolved.Select(r => r.Name).Contains(d.Key)) && resolved.Contains(p.Key) == false);
			var project = pair.Key;
			project.resolveDependencies(projects.Keys.ToArray(), pair.Value);
			resolved.Add(project);
		}
		var dag = new Dag(this.BuildNodes.ToArray());
		foreach (var dependency in this._dependencies)
		{
			dag.dependOf(dependency.buildGraph(projectsDescs, getExtFunc, selectedProject));
		}
		return dag;
	}

	private Dictionary<Project, Dictionary<string, bool>> buildProject(ProjectDescription[] projects, Func<FileType, string> getExtFunc)
	{
		return projects.Select(p => p.buildProject(this.ProjectActions, this._builder, getExtFunc)).ToDictionary();
	}

	public void printHelp()
	{
		Console.WriteLine($"Task: {this.Name}");
		Console.WriteLine(this.Description);
		if (this._paramHelps.Count > 0)
		{
			Console.WriteLine("Parameters:");
			foreach (var param in this._paramHelps)
			{
				Console.WriteLine($"  --{param.Key}: {param.Value}");
			}
		}
		if (this._dependencies.Length > 0)
		{
			Console.WriteLine("Depends on:");
			foreach (var dep in this._dependencies)
			{
				Console.WriteLine($"  {dep.Name}: {dep.Description}");
			}
		}
	}

	public void makeBuildNode(string key, string name, Func<IEnumerable<string>> dependencies, Func<IBuildContext, Task<ActionResult>> buildAction)
	{
		this.BuildNodes.Add(new DagNode(key, name, dependencies, buildAction));
	}
	public void makeBuildNode(string key, string name, Func<IEnumerable<string>> dependencies, Func<IBuildContext, ActionResult> buildAction)
	{
		this.makeBuildNode(key, name, dependencies, ctx =>
		{
			return System.Threading.Tasks.Task.FromResult(buildAction(ctx));
		});
	}
}