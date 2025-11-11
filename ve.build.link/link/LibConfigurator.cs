using ve.build.core;

namespace ve.build.link.link;

public interface ILibConfigurator
{
	ILibConfigurator extraFlag(string flag);
	ILibConfigurator removeFlag(Func<string, bool> predicate);
	Task run(IBuildContext ctx);
}