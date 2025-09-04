using ve.build.core.projects;

namespace ve.build.core.files;

public interface IFileHanle
{
	IFileHanle copy();
	IFileHanle copy(string outputPath);
}
internal class File : IFileHanle
{
	private readonly IProjectBuilder _builder;
	private readonly string _path;
	private readonly string _basePath;
	private readonly string[] _dependencies;

	public File(IProjectBuilder builder, string path, string basePath, string[] dependencies)
	{
		this._builder = builder;
		this._path = path;
		this._basePath = basePath;
		this._dependencies = dependencies;
	}
	public IFileHanle copy(string outputPath)
	{
		var fullPath = Path.Join(this._basePath, this._path);
		var output = Path.Join(outputPath, this._path)!;
		var key = $"copy:{fullPath}";
		this._builder.buildAction(key, $"Copy {this._path} to {output}", this._dependencies,
			ctx => System.IO.File.Copy(fullPath, output, true));
		return new File(this._builder, this._path, output, [key]);
	}

	public IFileHanle copy()
	{
		return this.copy(this._builder.OutputPath);
	}
}