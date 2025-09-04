namespace ve.build.core.platform;

public interface ITool
{
	string Name { get; }
	string Type { get; }
	void setup(IBuildContext ctx);
}
public interface IPlatformBuilder
{
	IPlatformBuilder makeTool<T>(T tool) where T : ITool;
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
}