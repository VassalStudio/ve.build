using ve.build.core.projects;

namespace ve.build.core.tasks;

public interface ITaskBuilder
{
	ITaskBuilder dependsOf(string name);
	ITaskBuilder eachProject(Action<IProjectBuilder> builderAction);
	ITaskBuilder buldAction(string key, string name, string[] dependencies, Action<IBuildContext> buildAction);
}
internal class TaskBuilder : ITaskBuilder
{
	private readonly List<string> _dependencies = new();
	private readonly List<Action<IBuildContext>> _parameters = new();
	private readonly Task _task;
	public TaskBuilder(string name, string description)
	{
		this._task = new Task(name, description);
		this.makeParam("help", false, (ctx, taskBuilder, value) => this._task.ShouldPrintHelp = value, "Print help for this task");
	}
	public ITaskBuilder dependsOf(string name)
	{
		this._dependencies.Add(name);
		return this;
	}

	public ITaskBuilder eachProject(Action<IProjectBuilder> builderAction)
	{
		this._task.ProjectActions = this._task.ProjectActions.Append(builderAction).ToArray();
		return this;
	}

	public ITaskBuilder buldAction(string key, string name, string[] dependencies, Action<IBuildContext> buildAction)
	{
		this._task.makeBuildNode(key, name, dependencies, buildAction);
		return this;
	}

	public Task build(TaskDescription[] tasks, IBuildContext ctx)
	{
		foreach (var param in this._parameters)
		{
			param(ctx);
		}
		return this._task.resolveDependencies(tasks.Where(t => this.Dependencies.Contains(t.Name)).Select(t => t.buildTask(tasks, ctx)).ToArray());
	}

	public ITaskBuilder makeParam<T>(string name, T defaultValue, Action<IBuildContext, ITaskBuilder, T> builder, Func<string, T> converter, string description)
	{
		this._parameters.Add(context =>
		{
			var value = context.Args.FirstOrDefault(arg => arg.StartsWith($"--{name}="));
			builder(context, this, value != null ? converter(value.Remove(0, $"--{name}".Length)) : defaultValue);
		});
		this._task._paramHelps[name] = description + $" (default: {defaultValue})";
		return this;
	}

	public ITaskBuilder makeParam(string name, bool defaultValue, Action<IBuildContext, ITaskBuilder, bool> builder,
		string description)
	{
		this._parameters.Add(context => builder(context, this, context.Args.Contains($"--{name}")));
		this._task._paramHelps[name] = description + $" (default: {defaultValue})";
		return this;
	}

	public string[] Dependencies => this._dependencies.ToArray();
}