using ve.build.cpp.cpp;
using File = ve.build.core.files.File;

namespace ve.build.cpp.msvc.msvc;

public class MsvcTool : ICppTool
{
	public MsvcTool(string cl, Action<IClConfigurator> configurator)
	{
		this.ClPath = cl;
		this.Configurator = configurator;
	}

	public Action<IClConfigurator> Configurator { get; }

	public string ClPath { get; }

	public IClConfigurator compile(File file, File obj)
	{
		return new ClConfigurator(this.ClPath, file, obj, Configurator);
	}

	public IScanDependenciesConfigurator scanDependencies(File inFile, File obj)
	{
		return new ShowDependenciesConfigurator(this.ClPath, inFile, obj, this.Configurator);
	}

	public string Name => "msvc";

}