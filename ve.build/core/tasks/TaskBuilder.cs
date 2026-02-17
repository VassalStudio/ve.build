using ve.build.core.projects;

using File = ve.build.core.files.File;

namespace ve.build.core.tasks;

public enum ActionResult
{
	SKIP,
	SUCCESS,
	FAILURE
}
public interface ITaskBuilder
{
	ITaskBuilder dependsOf(string name);
	ITaskBuilder eachProject(Action<IProjectBuilder> builderAction);
	ITaskBuilder buildAction(string key, string name, Func<IEnumerable<string>> dependencies, Func<IBuildContext, ActionResult> buildAction);
	ITaskBuilder buildAction(string key, string name, Func<IEnumerable<string>> dependencies, Func<IBuildContext, Task<ActionResult>> buildAction);
	ITaskBuilder copy(File inputFile, File outputFile, Func<string[]> deps);
	string Name { get; }
	IBuildContext BuildContext { get; }
}
internal class TaskBuilder : ITaskBuilder
{
	private readonly List<string> _dependencies = new();
	private readonly List<Action<IBuildContext>> _parameters = new();
	private readonly Task _task;

	public string Name => this._task.Name;
	public IBuildContext BuildContext { get; }

	public TaskBuilder(string name, string description, IBuildContext buildContext)
	{
		this._task = new Task(name, description, this);
		this.makeParam("help", false, (ctx, taskBuilder, value) => this._task.ShouldPrintHelp = value, "Print help for this task");
		this.BuildContext = buildContext;
	}
	public ITaskBuilder dependsOf(string name)
	{
		this._dependencies.Add(name);
		return this;
	}

	public ITaskBuilder eachProject(Action<IProjectBuilder> builderAction)
	{
		this._task.ProjectActions.Add(builderAction);
		return this;
	}

	public ITaskBuilder buildAction(string key, string name, Func<IEnumerable<string>> dependencies, Func<IBuildContext, Task<ActionResult>> buildAction)
	{
		this._task.makeBuildNode(key, name, dependencies, buildAction);
		return this;
	}

	public ITaskBuilder buildAction(string key, string name, Func<IEnumerable<string>> dependencies, Func<IBuildContext, ActionResult> buildAction)
	{
		this._task.makeBuildNode(key, name, dependencies, buildAction);
		return this;
	}

	public ITaskBuilder copy(File inputFile, File outputFile, Func<string[]> deps)
	{
		var key = $"copy:{outputFile.Path}";
		this.buildAction(key, $"Copy {inputFile.Path} to {outputFile.Path}", deps,
			ctx =>
			{
				try
				{
					System.IO.File.Copy(inputFile.Path, outputFile.Path, true);
					return ActionResult.SUCCESS;
				}
				catch (Exception ex)
				{
					ctx.log(LogLevel.FATAL, "COPY", ex);
					return ActionResult.FAILURE;
				}
			});
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
			var value = context.Args.FirstOrDefault(arg => arg.StartsWith($"--{name}="))?.Remove(0, $"--{name}=".Length);
			builder(context, this, value != null ? converter(value) : defaultValue);
			context.log(LogLevel.DEBUG, "PARAMS", $"--{name}={(value ?? defaultValue?.ToString())}");
		});
		this._task._paramHelps[name] = description + $" (default: {defaultValue})";
		return this;
	}

	public ITaskBuilder makeParam(string name, bool defaultValue, Action<IBuildContext, ITaskBuilder, bool> builder,
		string description)
	{
		this._parameters.Add(context =>
		{
			var value = context.Args.Contains($"--{name}");
			builder(context, this, value);
			if (value)
			{
				context.log(LogLevel.DEBUG, "PARAMS", $"--{name}");
			}
		});
		this._task._paramHelps[name] = description + $" (default: {defaultValue})";
		return this;
	}

	public string[] Dependencies => this._dependencies.ToArray();
}