using ve.build.core.projects;
using ve.build.link.link;

using File = ve.build.core.files.File;

namespace ve.build.link.msvc.msvc;

internal class MsvcLinkTool : ILinkTool
{
	public MsvcLinkTool(string cl, string libDir, string subsystem, Action<ILibConfigurator> libConfigurator, Action<ILinkConfigurator> linkConfigurator)
	{
		this.LinkPath = cl;
		this.LibDir = libDir;
		this.LibConfigurator = libConfigurator;
		this.LinkConfigurator = linkConfigurator;
		this.Subsystem = subsystem;
	}

	public string Subsystem { get; }

	public string LibDir { get; }

	public Action<ILinkConfigurator> LinkConfigurator { get; }

	public Action<ILibConfigurator> LibConfigurator { get; }

	public string LinkPath { get; }

	public ILibConfigurator lib(File file, File[] objs)
	{
		return new MsvcLibConfigurator(Path.Join(this.LinkPath, "lib.exe"), this.LibDir, this.Subsystem, file, objs, this.LibConfigurator);
	}

	public ILinkConfigurator link(File file, File[] objs)
	{
		return new MsvcLinkConfigurator(Path.Join(this.LinkPath, "link.exe"), this.LibDir, this.Subsystem, file, objs, this.LibConfigurator, this.LinkConfigurator);
	}

	public string getExt(FileType type)
	{
		return type switch
		{
			FileType.EXECUTABLE => ".exe",
			FileType.SHARED_LIBRARY => ".dll",
			FileType.STATIC_LIBRARY => ".lib",
			_ => throw new Exception($"Unsupported file type '{type}' for MSVC linker."),
		};
	}

	public string Name => "link";

}