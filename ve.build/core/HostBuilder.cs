using System.Runtime.CompilerServices;
using ve.build.core.platform;
using ve.build.core.projects;
using ve.build.core.tasks;

namespace ve.build.core;

public class HostBuilder
{
	private readonly Dictionary<string, TaskDescription> _tasks = new();
	private readonly Dictionary<string, ProjectDescription> _projects = new();
	private readonly List<Action<IBuildContext>> _parameters = new();
	private readonly Dictionary<string, string> _paramHelps = new();
	private readonly Dictionary<string, PlatformDesc> _platforms = new();
	public HostBuilder()
	{
		this.makeParam<LogLevel>("level", LogLevel.DEFAULT, (ctx, level) => (ctx as BuildContext)!.LogLevel = level, str => Enum.Parse<LogLevel>(str),
			"Set the log level. Possible values: FATAL, ERROR, WARN, INFO, DEBUG, VERBOSE");
		this.makeParam<Configuration>("config", Configuration.DEFAULT, (ctx, config) => (ctx as BuildContext)!.Configuration = config, str => Enum.Parse<Configuration>(str),
			"Set the build configuration. Possible values: DEBUG, RELEASE");
		this.makeParam("help", false, (ctx, value) => (ctx as BuildContext)!.ShouldPrintHelp = value, "Print help for the build tool");
		this.makeParam<string>("platform", "windows-x64",
			(ctx, platform) => (ctx as BuildContext)!.Platform = this._platforms[platform], s => s, 
			"Set the target platform. Example: windows-x64, linux-x64, osx-x64, etc");
	}

	public IBuildContext build()
	{
		return new BuildContext(this._tasks.ToArray(), this._projects.ToArray(), this._parameters.ToArray(), this._paramHelps.ToArray(), this._platforms.ToArray());
	}

	public HostBuilder task(string name, string description, Action<ITaskBuilder> builder)
	{
		if (this._tasks.TryGetValue(name, out var value) == false)
		{
			value = new TaskDescription(name, description, [builder]);
			this._tasks[name] = value;
		}
		else
		{
			value.Builder = value.Builder.Append(builder).ToArray();
		}
		return this;
	}

	public HostBuilder task(string name, string description)
	{
		return this.task(name, description, builder => { });
	}

	public HostBuilder project(string name, PROJECT_TYPE type)
	{
		return this.project(name, type, builder => { });
	}
	public HostBuilder project(string name, PROJECT_TYPE type, Action<IProjectBuilder> builder, [CallerFilePath]string? path = null)
	{
		if (this._projects.ContainsKey(name))
		{
			throw new ArgumentException($"Project with name '{name}' already exists.");
		}
		var projectDesc = new ProjectDescription(name, type, builder, Path.Join(Path.GetDirectoryName(path), "bin"));
		this._projects[name] = projectDesc;
		return this;
	}

	public HostBuilder platform(string name, bool isCurrent, Action<IPlatformBuilder> builder)
	{
		if (this._platforms.TryGetValue(name, out var value) == false)
		{
			value = new PlatformDesc(name, isCurrent, [builder]);
			this._platforms[name] = value;
		}
		else
		{
			value.Builder = value.Builder.Append(builder).ToArray();
		}
		return this;
	}

	public HostBuilder makeParam<T>(string name, T defaultValue, Action<IBuildContext, T> builder, Func<string, T> converter, string description)
	{
		this._parameters.Add(context =>
		{
			var value = context.Args.FirstOrDefault(arg => arg.StartsWith($"--{name}="));
			builder(context, value != null ? converter(value.Remove(0, $"--{name}".Length)) : defaultValue);
		});
		this._paramHelps[name] = description + $" (default: {defaultValue})";
		return this;
	}

	public HostBuilder makeParam(string name, bool defaultValue, Action<IBuildContext, bool> builder,
		string description)
	{
		this._parameters.Add(context => builder(context, context.Args.Contains($"--{name}")));
		this._paramHelps[name] = description + $" (default: {defaultValue})";
		return this;
	}
}