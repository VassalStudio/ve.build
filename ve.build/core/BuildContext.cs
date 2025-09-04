using System.Diagnostics;
using ve.build.core.projects;

namespace ve.build.core;

using ve.build.core.platform;
using ve.build.core.tasks;
public enum LogLevel
{
	FATAL,
	ERROR,
	WARN,
	INFO,
	DEBUG,
	VERBOSE,

#if DEBUG
	DEFAULT = DEBUG
#else
	DEFAULT = INFO
#endif
}

public enum Configuration
{
	DEBUG,
	RELEASE,
#if DEBUG
	DEFAULT = DEBUG
#else
	DEFAULT = RELEASE
#endif
}

public interface IBuildContext
{
	string[] Args { get; }
	LogLevel LogLevel { get; }
	Configuration Configuration { get; }
	void log(LogLevel level, string category, string message);
	void log(LogLevel level, string category, Exception ex);
	System.Threading.Tasks.Task run(string[] args);
}
internal class BuildContext : IBuildContext
{
	public string[] Args { get; private set; }
	public LogLevel LogLevel { get; set; } = LogLevel.DEFAULT;
	public Configuration Configuration { get; set; } = Configuration.DEFAULT;
	public PlatformDesc Platform { get; set; }
	private readonly Action<IBuildContext>[] Parameters;
	private readonly KeyValuePair<string, string>[] ParamHelps;
	internal BuildContext(KeyValuePair<string, TaskDescription>[] tasks,
		KeyValuePair<string, ProjectDescription>[] projects,
		Action<IBuildContext>[] parameters, KeyValuePair<string, string>[] paramHelps,
		KeyValuePair<string, PlatformDesc>[] platforms)
	{
		this.Tasks = tasks;
		this.Projects = projects;
		this.Parameters = parameters;
		this.ParamHelps = paramHelps;
		this.Platform = platforms.FirstOrDefault(p => p.Value.Platform.Platform.IsCurrent).Value;
	}

	internal KeyValuePair<string, TaskDescription>[] Tasks { get; }
	internal KeyValuePair<string, ProjectDescription>[] Projects { get; }
	public bool ShouldPrintHelp { get; set; }

	public async System.Threading.Tasks.Task run(string[] args)
	{
		this.Args = args;
		foreach (var param in this.Parameters)
		{
			param(this);
		}
		var taskName = args.FirstOrDefault(arg => arg.StartsWith("-") == false);
		if (taskName == null)
		{
			this.log(LogLevel.WARN, "BUILD", "No task specified. Use --help to see available tasks.");
			this.ShouldPrintHelp = true;
		}
		var taskDesc = taskName != null ? this.Tasks.FirstOrDefault(t => string.Equals(t.Key, taskName)).Value : null;
		if (taskName != null && taskDesc == null)
		{
			this.log(LogLevel.FATAL, "BUILD", $"Task '{taskName}' not found. Use --help to see available tasks.");
			this.ShouldPrintHelp = true;
		}
		if (this.ShouldPrintHelp)
		{
			this._printHelp();
		}
		else
		{
			try
			{
				var task = this.Platform.buildPlatform().buildTask(taskDesc!, this.Tasks.Select(t => t.Value).ToArray(), this);
				var projectsDescs = this.Projects.Select(p => p.Value).ToArray();
				var graph = task.buildGraph(projectsDescs.ToList());
				await graph.build(this);
			}
			catch(Exception ex)
			{
				this.log(LogLevel.FATAL, "BUILD", $"Task '{taskName}' failed:");
				this.log(LogLevel.FATAL, "BUILD", ex);
			}
		}
	}

	private void _printHelp()
	{
		this.log(LogLevel.INFO, "BUILD", "Available tasks:");
		foreach (var task in this.Tasks)
		{
			this.log(LogLevel.INFO, "BUILD", $"{task.Key}: {task.Value.Description}");
		}
		this.log(LogLevel.INFO, "BUILD", "Use `ve.build <task-name> [options]` to run a task.");
		this.log(LogLevel.INFO, "BUILD", "Use `ve.build <task-name> --help` to see task-specific help.");
		this.log(LogLevel.INFO, "BUILD", "Use `ve.build --help` to see this help message.");
	}

	public void log(LogLevel level, string category, string message)
	{
		if (level <= this.LogLevel)
		{
			var prefix = level switch
			{
				LogLevel.FATAL => "[FATAL]",
				LogLevel.ERROR => "[ERROR]",
				LogLevel.WARN => "[WARN]",
				LogLevel.INFO => "[INFO]",
				LogLevel.DEBUG => "[DEBUG]",
				LogLevel.VERBOSE => "[VERBOSE]",
				_ => "[UNKNOWN]"
			};
			var dateTime = DateTime.Now.ToString("G");
			var finalMessage = $"[{dateTime}]{prefix}[{category}] {message}";
			var prevColor = Console.ForegroundColor;
			Console.ForegroundColor = level switch
			{
				LogLevel.FATAL => ConsoleColor.DarkRed,
				LogLevel.ERROR => ConsoleColor.Red,
				LogLevel.WARN => ConsoleColor.Yellow,
				LogLevel.INFO => ConsoleColor.White,
				LogLevel.DEBUG => ConsoleColor.Gray,
				LogLevel.VERBOSE => ConsoleColor.DarkGray,
				_ => Console.ForegroundColor
			};
			Console.WriteLine(finalMessage);
			Console.ForegroundColor = prevColor;
#if DEBUG
			Debug.WriteLine(finalMessage);
#endif
		}
	}
	public void log(LogLevel level, string category, Exception ex)
	{
		this.log(level, category, ex.Message);
		foreach (var frame in new StackTrace(ex, true).GetFrames())
		{
			var method = frame.GetMethod();
			var fileName = frame.GetFileName() ?? "unknown file";
			var lineNumber = frame.GetFileLineNumber();
			this.log(level, category, $"\t{method?.DeclaringType?.FullName ?? "unknown type"}.{method?.Name} at {fileName}:{lineNumber}");
		}
		if (ex.InnerException != null)
		{
			this.log(level, category, "Inner Exception:");
			this.log(level, category, ex.InnerException);
		}
	}
}