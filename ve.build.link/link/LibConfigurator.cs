using ve.build.core;
using ve.build.core.tasks;

namespace ve.build.link.link;

public interface ILibConfigurator
{
	string[] Args { get; }
	ILibConfigurator extraFlag(string flag);
	ILibConfigurator removeFlag(Func<string, bool> predicate);
	Task<ActionResult> run(IBuildContext ctx);
}