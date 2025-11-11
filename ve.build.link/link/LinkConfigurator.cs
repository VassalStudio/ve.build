namespace ve.build.link.link;

public enum DebugInformation
{
	NONE,
	FASTLINK,
	FULL
}
public interface ILinkConfigurator : ILibConfigurator
{
	new ILinkConfigurator extraFlag(string flag);
	new ILinkConfigurator removeFlag(Func<string, bool> predicate);
	ILinkConfigurator enableDebugInformation(DebugInformation debugInformation);
	ILibConfigurator enableASLR(bool enable);
	ILinkConfigurator align(int alignment);
	ILinkConfigurator baseAddress(ulong address);
	ILinkConfigurator entryPoint(string? symbol);
	ILinkConfigurator export(string symbol);
	ILinkConfigurator import(string symbol);
	ILinkConfigurator ltcg(bool enable);
	ILinkConfigurator nodefaultLib();
	ILinkConfigurator nodefaultLib(string name);
	ILinkConfigurator map(bool enable);
	ILinkConfigurator wholeArchive();
	ILinkConfigurator wholeArchive(string libName);
	ILinkConfigurator wx(bool enable);
	ILinkConfigurator stack(int reserveSize, int commitSize);
	ILinkConfigurator heap(int reserveSize, int commitSize);
	ILinkConfigurator dynamicLibrary(bool enable);
}