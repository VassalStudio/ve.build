using System.Text.Json.Serialization;

namespace ve.build.cpp.msvc.msvc;

internal struct RequireDependency
{
	[JsonPropertyName("logical-name")]
	public string LogicalName { get; set; }
}
internal struct RuleDependency
{
	public RuleDependency()
	{
	}

	[JsonPropertyName("requires")]
	public RequireDependency[] Requires { get; set; } = [];
	[JsonPropertyName("provides")]
	public RequireDependency[] Provides { get; set; } = [];
}
internal struct Dependency
{
	public Dependency()
	{
	}

	[JsonPropertyName("rules")]
	public RuleDependency[] Rules { get; set; } = [];

	public static Dependency FromJson(string json)
	{
		return System.Text.Json.JsonSerializer.Deserialize<Dependency>(json);
	}
}
