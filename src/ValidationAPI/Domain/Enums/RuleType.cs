using System.Text.Json.Serialization;

namespace ValidationAPI.Domain.Enums;

public enum RuleType
{
	[JsonStringEnumMemberName("<")]  Less,
	[JsonStringEnumMemberName(">")]  More,
	[JsonStringEnumMemberName("<=")] LessOrEqual,
	[JsonStringEnumMemberName(">=")] MoreOrEqual,
	[JsonStringEnumMemberName("==")] Equal,
	[JsonStringEnumMemberName("!=")] NotEqual,
	Between,
	Outside,
	Regex,
	Email
}
