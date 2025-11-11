using ve.build.core.platform;
using ve.build.core.projects;
using File = ve.build.core.files.File;

namespace ve.build.link.link;

public interface ILinkTool : ITool
{
	ILibConfigurator lib(File file, File[] objs);
	ILinkConfigurator link(File file, File[] objs);
	string getExt(FileType type);
}