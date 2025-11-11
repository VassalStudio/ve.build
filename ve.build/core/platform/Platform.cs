using ve.build.core.projects;
using ve.build.core.tasks;
using Task = ve.build.core.tasks.Task;

namespace ve.build.core.platform;

internal class Platform
{
	public string Name { get; }

	public bool IsCurrent { get; }
	public readonly Dictionary<Type, ITool> DefaultTools = new();
	public readonly Dictionary<FileType, string> Extensions = new();
	public Platform(string name, bool isCurrent)
	{
		this.Name = name;
		this.IsCurrent = isCurrent;
	}

	public Task buildTask(TaskDescription taskDesc, TaskDescription[] allTasks, IBuildContext context)
	{
		return taskDesc!.buildTask(allTasks, context);
	}

	public void makeTool(Type type, ITool tool)
	{
		this.DefaultTools[type] = tool;
	}

	public string getExt(FileType type)
	{
		return this.Extensions.TryGetValue(type, out var value) ? value : throw new Exception($"Extension for file type '{type}' not found.");
	}
}