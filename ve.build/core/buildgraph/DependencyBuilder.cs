namespace ve.build.core.buildgraph;

internal class DependencyBuilder : IDependencyBuilder
{
	private readonly List<Dependency> _dependencies = new();
	private Exception? _error;

	public DependencyBuilder(DagNode node, IEnumerable<string> finished, IEnumerable<string> nonFinished)
	{
		this.Node = node;
		foreach (var dependency in this.Node.Dependencies)
		{
			try
			{
				dependency(this);
			}
			catch (Exception ex)
			{
				this.makeError(ex.Message);
				break;
			}
		}
		this.IsReady = this._error == null && this._dependencies.All(d => d.compile(finished, nonFinished));
	}

	public DagNode Node { get; }

	public bool IsReady { get; }
	public IDependencyBuilder makeEqualDependency(string key)
	{
		this._dependencies.Add(new EqualDependency(this, key));
		return this;
	}

	public IDependencyBuilder makeGroup(string type, Func<string, bool> dep)
	{
		this._dependencies.Add(new GroupDependency(this, type, dep));
		return this;
	}

	public IDependencyBuilder makeGroup(string type)
	{
		return this.makeGroup(type, _ => true);
	}

	public IDependencyBuilder makeError(string error)
	{
		this._error = new Exception(error, this._error);
		return this;
	}

	public void throwIfError()
	{
		if (this._error != null)
		{
			throw this._error;
		}
	}
}

internal abstract class Dependency(DependencyBuilder builder) : IDependency
{
	private Exception? _exception = null;
	public abstract bool compile(IEnumerable<string> finished, IEnumerable<string> nonFinished);

	protected void setError(string exception)
	{
		builder.makeError(exception);
	}
}

internal class EqualDependency(DependencyBuilder builder, string key) : Dependency(builder)
{
	private readonly string _key = key;
	public override bool compile(IEnumerable<string> finished, IEnumerable<string> nonFinished)
	{
		if (finished.Contains(this._key))
		{
			return true;
		}

		if (nonFinished.Contains(this._key) == false)
		{
			this.setError($"Dependency not found: {this._key}");
		}
		return false;
	}
}
internal class GroupDependency(DependencyBuilder builder, string type, Func<string, bool> dep) : Dependency(builder)
{
	private readonly string _type = type;
	private readonly Func<string, bool> _dep = dep;
	public override bool compile(IEnumerable<string> finished, IEnumerable<string> nonFinished)
	{
		var hasNonFinished = nonFinished.Any(this._hasDependency);
		if (hasNonFinished == false && finished.Any(this._hasDependency))
		{
			return true;
		}
		if (hasNonFinished == false)
		{
			this.setError($"Dependency group not found: {this._type}");
		}
		return false;
	}

	private bool _hasDependency(string arg)
	{
		var index = arg.IndexOf(':');
		if (index == -1)
		{
			return false;
		}
		var split = new String[] {arg.Substring(0, index), arg.Substring(index + 1)};
		return string.Equals(split[0], this._type) && this._dep(split[1]);
	}
}