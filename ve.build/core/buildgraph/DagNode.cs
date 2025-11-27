using ve.build.core.tasks;
using Task = System.Threading.Tasks.Task;

namespace ve.build.core.buildgraph;

internal class DagNode
{
	private readonly List<Func<IEnumerable<string>>> _dependenciesConstuctor = new();
	public string Key { get; }
	public string Name { get; }
	public Func<IBuildContext, Task<ActionResult>> BuildAction { get; }
	public string[] Dependencies => this._dependenciesConstuctor.SelectMany(d => d()).ToArray();
	public DagNode(string key, string name, Func<IEnumerable<string>> dependencies, Func<IBuildContext, Task<ActionResult>> buildAction)
	{
		this.Key = key;
		this.Name = name;
		this.BuildAction = buildAction;
		this._dependenciesConstuctor.Add(dependencies);
	}

	internal void makeDependencies(Func<string[]> value)
	{
		this._dependenciesConstuctor.Add(value);
	}

	public void dependOf(DagNode[] buildGraphNodes)
	{
		this._dependenciesConstuctor.Add(() => buildGraphNodes.Select(n => n.Key));
	}
}