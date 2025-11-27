using ve.build.core.projects;

namespace ve.build.core.platform;

public class PlatformDesc
{
	internal Action<IPlatformBuilder>[] Builder { get; set; }

	internal PlatformBuilder Platform { get; }
	public string Name => this.Platform.Platform.Name;

	public PlatformDesc(string name, bool isCurrent, Action<IPlatformBuilder>[] builder)
	{
		this.Builder = builder;
		this.Platform = new PlatformBuilder(name, isCurrent);
	}


	internal Platform buildPlatform()
	{
		foreach (var builder in this.Builder)
		{
			builder(this.Platform);
		}
		return this.Platform.Platform;
	}

	public string getExtension(FileType fileType)
	{
		return this.Platform.Platform.Extensions[fileType];
	}
}