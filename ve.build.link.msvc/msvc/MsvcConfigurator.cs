using System.Diagnostics;
using ve.build.core;
using ve.build.link.link;

using File = ve.build.core.files.File;

namespace ve.build.link.msvc.msvc;

internal class MsvcLibConfigurator : ILibConfigurator
{
	public MsvcLibConfigurator(string libPath, string libsDir, string subsystem, File file, File[] objs, Action<ILibConfigurator> configurator)
	{
		this.LibPath = libPath;
		this.extraFlag($"/OUT:{file.Path}").extraFlag("/nologo");
		foreach (var obj in objs)
		{
			this.extraFlag(obj.Path);
		}
		this.extraFlag($"/LIBPATH:{libsDir}").extraFlag($"/SUBSYSTEM:{subsystem}");
		if (Path.GetExtension(file.Path) == ".dll")
		{
			this.extraFlag("/DLL");
		}
		configurator(this);
	}

	public string LibPath { get; }

	private string[] _arguments = [];
	public ILibConfigurator extraFlag(string flag)
	{
		this._arguments = this._arguments.Append(flag).ToArray();
		return this;
	}

	public ILibConfigurator removeFlag(Func<string, bool> predicate)
	{
		this._arguments = this._arguments.Where(arg => predicate(arg) == false).ToArray();
		return this;
	}

	public async Task run(IBuildContext ctx)
	{
		var pi = new ProcessStartInfo(this.LibPath, this._arguments);
		pi.RedirectStandardOutput = true;
		pi.RedirectStandardError = true;
		pi.UseShellExecute = false;
		pi.CreateNoWindow = true;
		using var process = Process.Start(pi);
		await process!.WaitForExitAsync();
		var output = await process.StandardOutput.ReadToEndAsync();
		var error = await process.StandardError.ReadToEndAsync();
		if (process.ExitCode != 0)
		{
			ctx.log(LogLevel.ERROR, "MSVC", "Compilation failed:");
			foreach (var s in error.Split("\n").Where(s => string.IsNullOrWhiteSpace(s) == false))
			{
				ctx.log(LogLevel.ERROR, "MSVC", s);
			}
		}
		foreach (var s in output.Split("\n").Where(s => string.IsNullOrWhiteSpace(s) == false))
		{
			if (s.Trim() == System.IO.Path.GetFileName(this.LibPath)) continue;
			ctx.log(s.Contains("fatal error") ? LogLevel.FATAL : (s.Contains("error") ? LogLevel.ERROR : (s.Contains("warning") ? LogLevel.WARN : LogLevel.INFO)), "MSVC", s);
		}
	}
}

internal class MsvcLinkConfigurator : MsvcLibConfigurator, ILinkConfigurator
{
	public MsvcLinkConfigurator(string libPath, string libDir, string subsystem, File file, File[] objs, Action<ILibConfigurator> configurator, Action<ILinkConfigurator> linkConfigurator) 
		: base(libPath, libDir, subsystem, file, objs, configurator)
	{
		linkConfigurator(this);
	}
	private ILinkConfigurator _selectArgs<T>(T value, Dictionary<T, string?> map) where T : notnull
	{
		if (map.TryGetValue(value, out var arg))
		{
			this.removeFlag(map.ContainsValue);
			if (arg != null)
			{
				this.extraFlag(arg);
			}
		}
		return this;
	}

	public new ILinkConfigurator extraFlag(string flag)
	{
		return (ILinkConfigurator)base.extraFlag(flag);
	}

	public new ILinkConfigurator removeFlag(Func<string, bool> predicate)
	{
		return (ILinkConfigurator)base.removeFlag(predicate);
	}

	public ILinkConfigurator enableDebugInformation(DebugInformation debugInformation)
	{
		return this._selectArgs(debugInformation, new() {
			[DebugInformation.NONE] = null,
			[DebugInformation.FASTLINK] = "/DEBUG:FASTLINK",
			[DebugInformation.FULL] = "/DEBUG:FULL"
		});
	}

	public ILibConfigurator enableASLR(bool enable)
	{
		return this._selectArgs(enable, new() {
			[true] = "/DYNAMICBASE",
			[false] = "/DYNAMICBASE:NO"
		});
	}

	public ILinkConfigurator align(int alignment)
	{
		return this.removeFlag(arg => arg.StartsWith("/ALIGN:")).extraFlag($"/ALIGN:{alignment}");
	}

	public ILinkConfigurator baseAddress(ulong address)
	{
		return this.removeFlag(arg => arg.StartsWith("/BASE:")).extraFlag($"/BASE:0x{address:X}");
	}

	public ILinkConfigurator entryPoint(string? symbol)
	{
		return this.removeFlag(arg => arg.StartsWith("/ENTRY:")).extraFlag(symbol != null ? $"/ENTRY:{symbol}" : "/NOENTRY");
	}

	public ILinkConfigurator export(string symbol)
	{
		return this.extraFlag($"/EXPORT:{symbol}");
	}

	public ILinkConfigurator import(string symbol)
	{
		return this.extraFlag($"/IMPORT:{symbol}");
	}

	public ILinkConfigurator ltcg(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/LTCG",
			[false] = null
		});
	}

	public ILinkConfigurator nodefaultLib()
	{
		return this.extraFlag("/NODEFAULTLIB");
	}

	public ILinkConfigurator nodefaultLib(string name)
	{
		return this.extraFlag($"/NODEFAULTLIB:{name}");
	}

	public ILinkConfigurator map(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/MAP",
			[false] = null
		});
	}

	public ILinkConfigurator wholeArchive()
	{
		return this.extraFlag("/WHOLEARCHIVE");
	}

	public ILinkConfigurator wholeArchive(string libName)
	{
		return this.extraFlag($"/WHOLEARCHIVE:{libName}");
	}

	public ILinkConfigurator wx(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/WX",
			[false] = null
		});
	}

	public ILinkConfigurator stack(int reserveSize, int commitSize)
	{
		return this.removeFlag(arg => arg.StartsWith("/STACK:")).extraFlag($"/STACK:{reserveSize},{commitSize}");
	}

	public ILinkConfigurator heap(int reserveSize, int commitSize)
	{
		return this.removeFlag(arg => arg.StartsWith("/HEAP:")).extraFlag($"/HEAP:{reserveSize},{commitSize}");
	}

	public ILinkConfigurator dynamicLibrary(bool enable)
	{
		return this._selectArgs(enable, new()
		{
			[true] = "/DLL",
			[false] = null
		});
	}
}