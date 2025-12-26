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
				var readyToBuild = this.Nodes.Where(n => n.Dependencies.All(d => finishedNodes.Contains(d))).FirstOrDefault();
				if (readyToBuild == null)
				{
					throw new Exception("Cyclic dependency detected in build graph");
				}
				var result = await this._makeTask(ctx, readyToBuild);
				ctx.log(result.Value switch
					{
						ActionResult.SUCCESS => LogLevel.INFO,
						ActionResult.SKIP => LogLevel.VERBOSE,
						ActionResult.FAILURE => LogLevel.ERROR,
						_ => throw new NotImplementedException(),
					}, "BUILD", $"[{count - this.Nodes.Length + 1}/{count}] {readyToBuild.Name}");
				if (result.Value == ActionResult.FAILURE)
				{
					throw new Exception("Build failed");
				}
				finishedNodes.Add(readyToBuild.Key);
				this.Nodes = this.Nodes.Where(n => n != readyToBuild).ToArray();
			}
		}
		else
		{
			var completed = 0;
			var activeWrappers = new List<Task<KeyValuePair<DagNode, ActionResult>>>();
			var whenAny = async () =>
			{
				var finishedTask = await Task.WhenAny(activeWrappers);
				activeWrappers.Remove(finishedTask);
				completed++;
				try
				{
					var (node, result) = await finishedTask;
					ctx.log(result switch
						{
							ActionResult.SUCCESS => LogLevel.INFO,
							ActionResult.SKIP => LogLevel.VERBOSE,
							ActionResult.FAILURE => LogLevel.ERROR,
							_ => throw new NotImplementedException(),
						}, "BUILD", $"[{completed}/{count}] {node.Name}");
					finishedNodes.Add(node.Key);
				}
				catch (Exception ex)
				{
					ctx.log(LogLevel.ERROR, "BUILD", $"Task failed: {ex.Message}");
					throw;
				}
			};
			while (this.Nodes.Length > 0 || activeWrappers.Count > 0)
			{
				var readyToBuild = this.Nodes
					.Where(n => n.Dependencies.All(d => finishedNodes.Contains(d)))
					.ToArray();
				if (readyToBuild.Length == 0)
				{
					if (activeWrappers.Count > 0)
					{
						await whenAny();
					}
					else
					{
						throw new Exception("Cyclic dependency detected or logic error");
					}
				}
				foreach (var node in readyToBuild)
				{
					if (activeWrappers.Count >= threads)
					{
						await whenAny();
					}
					activeWrappers.Add(this._makeTask(ctx, node));
				}
				this.Nodes = this.Nodes.Where(n => !readyToBuild.Contains(n)).ToArray();
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