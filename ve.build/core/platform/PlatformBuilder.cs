namespace ve.build.core.platform;

public interface IPlatformBuilder
{
	
}
internal class PlatformBuilder : IPlatformBuilder
{
	public PlatformBuilder(string name, bool isCurrent)
	{
		this.Platform = new Platform(name, isCurrent);
	}

	public Platform Platform { get; }
}