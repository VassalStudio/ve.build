namespace ve.build.core.projects;

public enum PROJECT_TYPE
{
	APPLICATION,
	SHARED_LIBRARY,
	STATIC_LIBRARY
}
public interface IProjectBuilder
{
	IProjectBuilder buildAction(string key, string name, string[] dependencies, Action<IBuildContext> buildAction);
	IProjectBuilder dependsOf(string name, bool isPublic = false);
}
internal class ProjectBuilder : IProjectBuilder
{
	private Project _project;
	public readonly Dictionary<string, bool> Dependencies = new();
	public ProjectBuilder(string name, PROJECT_TYPE type)
	{
		this._project = new Project(name, type);
	}
	public KeyValuePair<Project, Dictionary<string, bool>> build()
	{
		return new KeyValuePair<Project, Dictionary<string, bool>>(this._project, this.Dependencies);
	}

	public IProjectBuilder buildAction(string key, string name, string[] dependencies, Action<IBuildContext> buildAction)
	{
		this._project.makeBuildNode(key, name, dependencies, buildAction);
		return this;
	}

	public IProjectBuilder dependsOf(string name, bool isPublic = false)
	{
		this.Dependencies[name] = isPublic;
		return this;
	}
}