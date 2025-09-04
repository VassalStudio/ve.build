namespace ve.build.core.buildgraph;

internal class DagNode
{
	public string Key { get; }
	public string Name { get; }
	public Action<IBuildContext> BuildAction { get; }
	public List<string> Dependencies { get; } = new();
	public DagNode(string key, string name, string[] dependencies, Action<IBuildContext> buildAction)
	{
		this.Key = key;
		this.Name = name;
		this.BuildAction = buildAction;
	}
}