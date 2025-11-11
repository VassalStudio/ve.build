using ve.build.core.projects;

namespace ve.build.core.files;

public class File
{
	private readonly string _path;
	private string[] _dependencies;
	private readonly Func<FileType, string> _extensionGetter;

	public File(string path, Func<FileType, string> extentionGetter)
	{
		this._path = path;
		this._dependencies = [];
		this._extensionGetter = extentionGetter;
	}

	public string Path => this._path;
	public string[] Dependencies => this._dependencies;

	public void addDependencies(params string[] deps)
	{
		this._dependencies = this._dependencies.Concat(deps).ToArray();
	}

	public File changeExtension(FileType type)
	{
		return new File(System.IO.Path.ChangeExtension(this.Path, this._extensionGetter(type)), this._extensionGetter);
	}
}