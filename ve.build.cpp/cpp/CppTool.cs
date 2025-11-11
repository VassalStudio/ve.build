using ve.build.core.platform;
using File = ve.build.core.files.File;

namespace ve.build.cpp.cpp;

public interface ICppTool : ITool
{
	IClConfigurator compile(File file, File obj);
}