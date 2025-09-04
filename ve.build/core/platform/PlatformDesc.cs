namespace ve.build.core.platform;

internal class PlatformDesc
{
	public Action<IPlatformBuilder>[] Builder { get; set; }
	public PlatformBuilder Platform { get; }

	public PlatformDesc(string name, bool isCurrent, Action<IPlatformBuilder>[] builder)
	{
		this.Builder = builder;
		this.Platform = new PlatformBuilder(name, isCurrent);
	}


	public Platform buildPlatform()
	{
		foreach (var builder in this.Builder)
		{
			builder(this.Platform);
		}
		return this.Platform.Platform;
	}
}