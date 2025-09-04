namespace ve.build.core.tasks;

internal class TaskDescription
{
	public string Name { get; }
	public string Description { get; }
	public Action<TaskBuilder>[] Builder { get; set; }
	public TaskDescription(string name, string description, Action<TaskBuilder>[] builder)
	{
		this.Name = name;
		this.Description = description;
		this.Builder = builder;
	}
	public TaskDescription(string name, string description) : this(name, description, [])
	{
	}

	public Task buildTask(TaskDescription[] tasks, IBuildContext ctx)
	{
		var taskBuilder = new TaskBuilder(this.Name, this.Description);
		foreach (var builder in this.Builder)
		{
			builder(taskBuilder);
		}
		return taskBuilder.build(tasks, ctx);
	}
}