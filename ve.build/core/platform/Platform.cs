using ve.build.core.tasks;
using Task = ve.build.core.tasks.Task;

namespace ve.build.core.platform;

internal class Platform
{
	public string Name { get; }

	public bool IsCurrent { get; }
	public Platform(string name, bool isCurrent)
	{
		this.Name = name;
		this.IsCurrent = isCurrent;
	}

	public Task buildTask(TaskDescription taskDesc, TaskDescription[] allTasks, IBuildContext context)
	{
		return taskDesc!.buildTask(allTasks, context);
	}
}