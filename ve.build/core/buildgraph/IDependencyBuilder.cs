namespace ve.build.core.buildgraph;

public interface IDependencyBuilder
{
	IDependencyBuilder makeEqualDependency(string key);
	IDependencyBuilder makeGroup(string type, Func<string, bool> dep);
	IDependencyBuilder makeGroup(string type);
	IDependencyBuilder makeError(string error);
}

public interface IDependency
{

}