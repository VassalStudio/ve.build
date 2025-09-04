using ve.build.core.buildgraph;
using ve.build.core.projects;

namespace ve.build.core.tasks;

internal class Task
{
	private Task[] _dependencies;
	public readonly Dictionary<string, string> _paramHelps = new();
	public readonly List<DagNode> BuildNodes = new();

	public Task(string name, string description)
	{
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
				node.Dependencies.AddRange(task.BuildNodes.Select(n => n.Key));
			}
		}
		return this;
	}

	public string Name { get; }
	public string Description { get; }
	public bool ShouldPrintHelp { get; set; }

	public Dag buildGraph(List<ProjectDescription> projectsDescs)
	{
		Dictionary<Project, Dictionary<string, bool>> projects = new();
		while (projectsDescs.Count > 0)
		{
			var built = projectsDescs[0].buildProject(this.ProjectActions
				.Concat(this._dependencies.SelectMany(d => d.ProjectActions)).ToArray());
			projects.Add(built.Key, built.Value);
			projectsDescs.RemoveAt(0);
		}

		foreach (var project in projects)
		{
			project.Key.resolveDependencies(projects.Keys.ToArray(), project.Value);
		}
		return new Dag(this.BuildNodes.Concat(projects.SelectMany(p => p.Key.BuildNode)).ToArray());
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

	public void makeBuildNode(string key, string name, string[] dependencies, Func<IBuildContext, System.Threading.Tasks.Task> buildAction)
	{
		this.BuildNodes.Add(new DagNode(key, name, dependencies, buildAction));
	}
	public void makeBuildNode(string key, string name, string[] dependencies, Action<IBuildContext> buildAction)
	{
		this.makeBuildNode(key, name, dependencies, async ctx =>
		{
			buildAction(ctx);
			await System.Threading.Tasks.Task.CompletedTask;
		});
	}
}