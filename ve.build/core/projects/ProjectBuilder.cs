using ve.build.core.tasks;
using File = ve.build.core.files.File;

namespace ve.build.core.projects;

public enum FileType
{
	OBJECT, EXECUTABLE, SHARED_LIBRARY, STATIC_LIBRARY, DEBUG_SYMBOLS, MODULE_INTERFACE
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
	File intermediateFile(string path);
	File outputFile(string path);
	File intermediateFile(File path);
	File outputFile(File path);
	File[] SourceFiles { get; }
	IEnumerable<string> Dependencies { get; }
	IProjectBuilder sources(Action<File[]> files);
	IProjectBuilder makeSourceFile(File f);
	IProjectBuilder dependencies(Action<IReadOnlyDictionary<IProjectBuilder, bool>> dependencies);
}

internal class ProjectBuilder : IProjectBuilder
{
	private Project _project;
	private readonly Dictionary<string, bool> _dependencies = new();
	private readonly ITaskBuilder _builder;
	private readonly Func<FileType, string> _extFunc;
	private readonly List<File> _sourceFiles = new();
	private readonly List<Action<File[]>> _sourceCallbacks = new();
	private readonly List<Action<IReadOnlyDictionary<IProjectBuilder, bool>>> _dependencyCallbacks = new();

	public IReadOnlyList<Action<IReadOnlyDictionary<IProjectBuilder, bool>>> DependencyCallbacks =>
		this._dependencyCallbacks;
	public IReadOnlyList<Action<File[]>> SourceCallbacks => this._sourceCallbacks;

	public ProjectBuilder(ITaskBuilder builder, string name, string path, string outputPath, string intermediate, Func<FileType, string> getExtFunc)
	{
		this._builder = builder;
		this._extFunc = getExtFunc;
		this._project = new Project(name, path, System.IO.Path.Join(outputPath, this._builder.BuildContext.Platform.Name, this._builder.BuildContext.Configuration.ToString()),
			System.IO.Path.Join(intermediate, this._builder.BuildContext.Platform.Name, this._builder.BuildContext.Configuration.ToString()), this);
	}
	public KeyValuePair<Project, Dictionary<string, bool>> build()
	{
		foreach (var callback  in this._sourceCallbacks)
		{
			callback(this._sourceFiles.ToArray());
		}

		this._sourceCallbacks.RemoveAll(_ => true);
		return new KeyValuePair<Project, Dictionary<string, bool>>(this._project, this._dependencies);
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
		this._dependencies[name] = isPublic;
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

	public File intermediateFile(string path)
	{
		return new File(System.IO.Path.Join(this.Intermediate, path), this._extFunc);
	}

	public File outputFile(string path)
	{
		return new File(System.IO.Path.Join(this.OutputPath, path), this._extFunc);
	}

	public File intermediateFile(File path)
	{
		return new File( System.IO.Path.Join(this.Intermediate, System.IO.Path.GetRelativePath(this.Path, path.Path)), this._extFunc, path);
	}

	public File outputFile(File path)
	{
		return new File( System.IO.Path.Join(this.OutputPath, System.IO.Path.GetRelativePath(this.Path, path.Path)), this._extFunc, path);
	}

	public File[] SourceFiles => this._sourceFiles.ToArray();
	public IEnumerable<string> Dependencies => this._dependencies.Keys.Concat(this._dependencies.Where(k => k.Value).SelectMany(d => new string[]{}));

	public IProjectBuilder sources(Action<File[]> files)
	{
		this._sourceCallbacks.Add(files);
		return this;
	}

	public IProjectBuilder dependencies(Action<IReadOnlyDictionary<IProjectBuilder, bool>> dependencies)
	{
		this._dependencyCallbacks.Add(dependencies);
		return this;
	}

	public IProjectBuilder makeSourceFile(File f)
	{
		this._sourceFiles.Add(f);
		return this;
	}
}