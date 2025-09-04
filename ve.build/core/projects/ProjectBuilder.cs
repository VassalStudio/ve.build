using System.Runtime.CompilerServices;
using ve.build.core.files;
using File = ve.build.core.files.File;

namespace ve.build.core.projects;

public enum PROJECT_TYPE
{
	APPLICATION,
	SHARED_LIBRARY,
	STATIC_LIBRARY
}
public interface IProjectBuilder
{
	IProjectBuilder buildAction(string key, string name, string[] dependencies, Func<IBuildContext, Task> buildAction);
	IProjectBuilder buildAction(string key, string name, string[] dependencies, Action<IBuildContext> buildAction);
	IProjectBuilder dependsOf(string name, bool isPublic = false);
	IFileHanle file(string path, [CallerFilePath]string? fullPath = null);
	string OutputPath { get; }
	IProjectBuilder outputPath(string path);
}
internal class ProjectBuilder : IProjectBuilder
{
	private Project _project;
	public readonly Dictionary<string, bool> Dependencies = new();
	public ProjectBuilder(string name, PROJECT_TYPE type, string outputPath)
	{
		this._project = new Project(name, type);
		this.outputPath(outputPath);
	}
	public KeyValuePair<Project, Dictionary<string, bool>> build()
	{
		return new KeyValuePair<Project, Dictionary<string, bool>>(this._project, this.Dependencies);
	}

	public IProjectBuilder buildAction(string key, string name, string[] dependencies, Func<IBuildContext, Task> buildAction)
	{
		this._project.makeBuildNode(key, name, dependencies, buildAction);
		return this;
	}

	public IProjectBuilder buildAction(string key, string name, string[] dependencies, Action<IBuildContext> buildAction)
	{
		return this.buildAction(key, name, dependencies, async ctx =>
		{
			buildAction(ctx);
			await Task.CompletedTask;
		});
	}

	public IProjectBuilder dependsOf(string name, bool isPublic = false)
	{
		this.Dependencies[name] = isPublic;
		return this;
	}

	public IFileHanle file(string path, string? fullPath = null)
	{
		return new File(this, path, Path.GetDirectoryName(fullPath)!, []);
	}

	public string OutputPath { get; private set; }
	public IProjectBuilder outputPath(string path)
	{
		this.OutputPath = path;
		return this;
	}
}