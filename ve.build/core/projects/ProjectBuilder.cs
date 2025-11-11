using ve.build.core.tasks;
using File = ve.build.core.files.File;

namespace ve.build.core.projects;

public enum FileType
{
	OBJECT, EXECUTABLE, SHARED_LIBRARY, STATIC_LIBRARY, DEBUG_SYMBOLS
}

public interface IProjectBuilder
{
	IProjectBuilder task(string key, Action<ITaskBuilder> builder);
	IProjectBuilder dependsOf(string name, bool isPublic = false);
	string OutputPath { get; }
	string Intermediate { get; }
	string Path { get; }
	string Name { get; }
	File[] files(string directory, string searchPattern, SearchOption searchOption = SearchOption.AllDirectories);
	File file(string file);
	File intermediateFile(string path, FileType type);
	File outputFile(string path, FileType type);
	File intermediateFile(File path, FileType type);
	File outputFile(File path, FileType type);
	File[] SourceFiles { get; }
	IProjectBuilder sources(Action<File[]> files);
	IProjectBuilder makeSourceFile(File f);
}

internal class ProjectBuilder : IProjectBuilder
{
	private Project _project;
	public readonly Dictionary<string, bool> Dependencies = new();
	private readonly ITaskBuilder _builder;
	private readonly Func<FileType, string> _extFunc;
	private readonly List<File> _sourceFiles = new();
	private readonly List<Action<File[]>> _sourceCallbacks = new();

	public ProjectBuilder(ITaskBuilder builder, string name, string path, string outputPath, string intermediate, Func<FileType, string> getExtFunc)
	{
		this._builder = builder;
		this._extFunc = getExtFunc;
		this._project = new Project(name, path, outputPath, intermediate);
	}
	public KeyValuePair<Project, Dictionary<string, bool>> build()
	{
		foreach (var callback in this._sourceCallbacks)
		{
			callback(this._sourceFiles.ToArray());
		}
		return new KeyValuePair<Project, Dictionary<string, bool>>(this._project, this.Dependencies);
	}

	public IProjectBuilder task(string key, Action<ITaskBuilder> builder)
	{
		if (this._builder.Name == key)
		{
			builder(this._builder);
		}
		return this;
	}

	public IProjectBuilder dependsOf(string name, bool isPublic = false)
	{
		this.Dependencies[name] = isPublic;
		return this;
	}

	public string OutputPath => this._project.Output;
	public string Intermediate => this._project.Intermediate;
	public string Path => this._project.Path;
	public string Name => this._project.Name;

	public File file(string file)
	{
		return new File(System.IO.Path.Join(this.Path, file), this._extFunc);
	}

	public File[] files(string directory, string searchPattern, SearchOption searchOption = SearchOption.AllDirectories)
	{
		var dirPath = System.IO.Path.Join(this.Path, directory);
		return Directory.GetFiles(dirPath, searchPattern, searchOption)
			.Select(f => this.file(System.IO.Path.GetRelativePath(this.Path, f))).ToArray();
	}

	public File intermediateFile(string path, FileType type)
	{
		return new File(System.IO.Path.Join(this.Intermediate, path), this._extFunc).changeExtension(type);
	}

	public File outputFile(string path, FileType type)
	{
		return new File(System.IO.Path.Join(this.OutputPath, path), this._extFunc).changeExtension(type);
	}

	public File intermediateFile(File path, FileType type)
	{
		return this.intermediateFile(System.IO.Path.GetRelativePath(this.Path, path.Path), type);
	}

	public File outputFile(File path, FileType type)
	{
		return this.outputFile(System.IO.Path.GetRelativePath(this.Path, path.Path), type);
	}

	public File[] SourceFiles => this._sourceFiles.ToArray();

	public IProjectBuilder sources(Action<File[]> files)
	{
		this._sourceCallbacks.Add(files);
		return this;
	}

	public IProjectBuilder makeSourceFile(File f)
	{
		this._sourceFiles.Add(f);
		return this;
	}
}