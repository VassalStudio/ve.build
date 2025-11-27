using ve.build.core.projects;

namespace ve.build.core.files;

public class File
{
	private readonly Func<FileType, string> _extensionGetter;

	public File(string path, Func<FileType, string> extentionGetter, File? sourceFile)
	{
		this.Path = path;
		this._extensionGetter = extentionGetter;
		this.SourceFile = sourceFile;
	}

	public File(string path, Func<FileType, string> extentionGetter)
		: this(path, extentionGetter, null)
	{
	}

	public string Path { get; }
	public File? SourceFile { get; }
	public DateTime TimeStamp => System.IO.File.Exists(this.Path) ? System.IO.File.GetLastWriteTimeUtc(this.Path) : DateTime.UtcNow;
	public bool Exists => System.IO.File.Exists(this.Path);

	public File changeExtension(FileType type)
	{
		return new File(System.IO.Path.ChangeExtension(this.Path, this._extensionGetter(type)), this._extensionGetter, this.SourceFile ?? this);
	}

	public File changeExtension(FileType type, bool resetSource)
	{
		return resetSource ? new File(System.IO.Path.ChangeExtension(this.Path, this._extensionGetter(type)), this._extensionGetter, this) : this.changeExtension(type);
	}
}