using ve.build.core;
using ve.build.core.tasks;

namespace ve.build.cpp.cpp;

using File = ve.build.core.files.File;

public enum OptimizationLevel
{
	NONE,
	SIZE,
	SPEED,
	MAXIMUM
}
public enum InlineLevel
{
	NONE,
	DEFAULT,
	FORCE
}
public enum FavorOptimization
{
	NONE,
	SPEED,
	SIZE
}
public enum ExceptionHandling
{
	NONE,
	STRUCTURED,
	CXX,
	BOTH
}
public enum FloatModel
{
	FAST,
	PRECISE,
	STRICT
}
public enum SSEArch
{
	NONE,
	SSE,
	SSE2,
	SSE42,
	AVX,
	AVX2,
	AVX512,
	AVX10X
}
public enum VectorLength
{
	AUTO,
	V256,
	V512
}
public enum LanguageStandard
{
	// C
	C11,
	C17,
	CLatest,

	// C++
	Cpp14,
	Cpp17,
	Cpp20,
	CppLatest
}
public enum DebugInformationFormat
{
	NONE,
	INTERNAL,
	EXTERNAL
}

public interface IClConfigurator
{
	string[] Args { get; }
	IClConfigurator optimization(OptimizationLevel level);
	IClConfigurator inlineLevel(InlineLevel level);
	IClConfigurator enableIntrinsic(bool enable);
	IClConfigurator extraFlags(string flag);
	IClConfigurator removeFlags(string flag);
	IClConfigurator removeFlags(Func<string, bool> action);
	IClConfigurator splitSections(bool enable);
	IClConfigurator securityCheckers(bool enable);
	IClConfigurator rtti(bool enable);
	IClConfigurator favorOptimization(FavorOptimization favor);
	IClConfigurator exceptionHandling(ExceptionHandling handling);
	IClConfigurator floatModel(FloatModel model);
	IClConfigurator floatExceptions(bool enable);
	IClConfigurator floatContract(bool enable);
	IClConfigurator fastTranscendentals(bool enable);
	IClConfigurator linkTimeCodeGeneration(bool enable);
	IClConfigurator stackCheck(bool enable);
	IClConfigurator addressSanitizer(bool enable);
	IClConfigurator arch(SSEArch arch);
	IClConfigurator vectorLength(VectorLength length);
	IClConfigurator macro(string name, string? value = null);
	IClConfigurator forceInclude(File file);
	IClConfigurator removeMacro(string name);
	IClConfigurator includeDir(string path);
	IClConfigurator noSTDIncludes(bool enable);
	IClConfigurator languageStandard(LanguageStandard standard);
	IClConfigurator constexprDepth(int depth = 512);
	IClConfigurator constexprBacktrace(int backtrace = 5);
	IClConfigurator constexprSteps(int steps = 1048576);
	IClConfigurator debugInformationFormat(DebugInformationFormat format);
	IClConfigurator structPacking(int packing);
	IClConfigurator throwingNew(bool enable);
	IClConfigurator openMP(bool enable);
	Task<ActionResult> run(IBuildContext ctx);
	IClConfigurator module(string modulePath);
}

public interface IScanDependenciesConfigurator : IClConfigurator
{
	string[] Dependencies { get; }
	string[] Includes { get; }
	IReadOnlyDictionary<string, string> ProvidedDeps { get; }
}