using ve.build.core.projects;

namespace ve.build.core.platform;

public interface ITool
{
	string Name { get; }
}
public interface IPlatformBuilder
{
	IPlatformBuilder makeTool<T>(T tool) where T : ITool;
	IPlatformBuilder makeFileType(FileType fileType, string extension);
}
internal class PlatformBuilder : IPlatformBuilder
{
	public PlatformBuilder(string name, bool isCurrent)
	{
		this.Platform = new Platform(name, isCurrent);
	}

	public Platform Platform { get; }
	public IPlatformBuilder makeTool<T>(T tool) where T : ITool
	{
		this.Platform.makeTool(typeof(T), tool);
		return this;
	}

	public IPlatformBuilder makeFileType(FileType fileType, string extension)
	{
		this.Platform.Extensions[fileType] = extension;
		return this;
	}
}