namespace ve.build.core.buildgraph;

internal class Dag
{
	public Dag(DagNode[] nodes)
	{
		this.Nodes = nodes;
	}

	public DagNode[] Nodes { get; private set; }

	public Task build(IBuildContext ctx)
	{
#if DEBUG
		return this.build(ctx, 1);
#else
		return this.build(ctx, Environment.ProcessorCount);
#endif
	}

	public async Task build(IBuildContext ctx, int threads)
	{
		var count = this.Nodes.Length;
		var completed = 0;
		Dictionary<Task, DagNode> runningTasks = new();
		while (this.Nodes.Length > 0)
		{
			var readyToBuild = this.Nodes.Where(n => n.Dependencies.Count == 0).ToArray();
			if (readyToBuild.Length == 0)
			{
				throw new Exception("Cyclic dependency detected in build graph");
			}
			foreach (var node in readyToBuild)
			{
				if (runningTasks.Count >= threads)
				{
					var finished = await Task.WhenAny(runningTasks.Keys);
					runningTasks.Remove(finished);
					completed++;
					ctx.log(LogLevel.INFO, "BUILD", $"[{completed}/{count}] {node.Name}");
					foreach (var dagNode in this.Nodes)
					{
						dagNode.Dependencies.Remove(node.Key);
					}
				}
				runningTasks.Add(Task.Run(() => node.BuildAction(ctx)), node);
				this.Nodes = this.Nodes.Where(n => n != node).ToArray();
			}
		}
		await Task.WhenAll(runningTasks.Keys);
		foreach (var task in runningTasks)
		{
			completed++;
			ctx.log(LogLevel.INFO, "BUILD", $"[{completed}/{count}] {task.Value.Name}");
		}
	}
}