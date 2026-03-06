using ve.build.core.tasks;
using Task = System.Threading.Tasks.Task;

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
		ctx.log(LogLevel.DEBUG, "CORE", $"Run DAG with {threads} threads");
		var count = this.Nodes.Length;
		List<string> finishedNodes = new();
		if (threads <= 1)
		{
			while (this.Nodes.Length > 0)
			{
				var nonFinished = this.Nodes.Select(n => n.Key).ToArray();
				var deps = this.Nodes.Select(n => new DependencyBuilder(n, finishedNodes, nonFinished)).ToArray();
				var readyToBuild = deps.FirstOrDefault(n => n.IsReady);
				if (readyToBuild == null)
				{
					foreach (var dep in deps)
					{
						dep.throwIfError();
					}
					throw new Exception("Cycle detected in module dependencies at tasks:\n" + string.Join('\n', deps.Select(d => d.Node.Name)));
				}
				var result = await this._makeTask(ctx, readyToBuild.Node);
				ctx.log(result.Value switch
					{
						ActionResult.SUCCESS => LogLevel.INFO,
						ActionResult.SKIP => LogLevel.VERBOSE,
						ActionResult.FAILURE => LogLevel.ERROR,
						_ => throw new NotImplementedException(),
					}, "BUILD", $"[{count - this.Nodes.Length + 1}/{count}] {readyToBuild.Node.Name}");
				if (result.Value == ActionResult.FAILURE)
				{
					throw new Exception($"Task {readyToBuild.Node.Name} failed");
				}
				finishedNodes.Add(readyToBuild.Node.Key);
				this.Nodes = this.Nodes.Where(n => n != readyToBuild.Node).ToArray();
			}
		}
		else
		{
			var completed = 0;
			var activeWrappers = new List<Task<KeyValuePair<DagNode, ActionResult>>>();
			var nonFinished = this.Nodes.Select(n => n.Key).ToList();
			var whenAny = async () =>
			{
				var finishedTask = await Task.WhenAny(activeWrappers);
				activeWrappers.Remove(finishedTask);
				completed++;
				var (node, result) = await finishedTask;
				ctx.log(result switch
					{
						ActionResult.SUCCESS => LogLevel.INFO,
						ActionResult.SKIP => LogLevel.VERBOSE,
						ActionResult.FAILURE => LogLevel.ERROR,
						_ => throw new NotImplementedException(),
					}, "BUILD", $"[{completed}/{count}] {node.Name}");
				finishedNodes.Add(node.Key);
				if (result == ActionResult.FAILURE)
				{
					throw new Exception($"Task {node.Name} failed");
				}
				nonFinished.Remove(node.Key);
			};
			while (this.Nodes.Length > 0 || activeWrappers.Count > 0)
			{
				var deps = this.Nodes.Select(n => new DependencyBuilder(n, finishedNodes, nonFinished)).ToArray();
				var readyToBuild = deps.Where(n => n.IsReady).ToArray();
				if (readyToBuild.Length == 0)
				{
					if (activeWrappers.Count > 0)
					{
						await whenAny();
					}
					else
					{
						foreach (var dep in deps)
						{
							dep.throwIfError();
						}
						throw new Exception("Cycle detected in module dependencies at tasks:\n" + string.Join('\n', deps.Select(d => d.Node.Name)));
					}
				}
				foreach (var node in readyToBuild)
				{
					if (activeWrappers.Count >= threads)
					{
						await whenAny();
					}
					activeWrappers.Add(this._makeTask(ctx, node.Node));
				}
				this.Nodes = this.Nodes.Where(n => readyToBuild.Any(r => r.Node.Equals(n)) == false).ToArray();
			}
		}
	}

	private async Task<KeyValuePair<DagNode, ActionResult>> _makeTask(IBuildContext ctx, DagNode node)
	{
		return new KeyValuePair<DagNode, ActionResult>(node, await node.BuildAction(ctx));
	}

	public void dependOf(Dag buildGraph)
	{
		foreach (var node in this.Nodes)
		{
			node.dependOf(buildGraph.Nodes);
		}
		this.Nodes = this.Nodes.Concat(buildGraph.Nodes).ToArray();
	}
}