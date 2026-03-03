using ve.build.core.tasks;
using Task = System.Threading.Tasks.Task;

namespace ve.build.core.buildgraph;

internal class DagNode
{
	private readonly List<Action<IDependencyBuilder>> _dependenciesConstuctor = new();
	public string Key { get; }
	public string Name { get; }
	public Func<IBuildContext, Task<ActionResult>> BuildAction { get; }
	public Action<IDependencyBuilder>[] Dependencies => this._dependenciesConstuctor.ToArray();
	public DagNode(string key, string name, Action<IDependencyBuilder> dependencies, Func<IBuildContext, Task<ActionResult>> buildAction)
	{
		this.Key = key;
		this.Name = name;
		this.BuildAction = buildAction;
		this._dependenciesConstuctor.Add(dependencies);
	}

	internal void makeDependencies(Action<IDependencyBuilder> pred)
	{
		this._dependenciesConstuctor.Add(pred);
	}

	public void dependOf(DagNode[] buildGraphNodes)
	{
		foreach (var node in buildGraphNodes)
		{
			this.makeDependencies(p => p.makeEqualDependency(node.Key));
		}
	}
}