using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Common.Models;

public record PropertyRequest(PropertyType Type, bool IsOptional)
{
	private readonly RuleRequest[] _rules = null!;
	public RuleRequest[] Rules
	{ get => _rules; init => _rules = value ?? []; }
}

public static class PropertyExtensions
{
	public static PropertyRequest ToRequest(this Property property, RuleRequest[] rules)
		=> new (property.Type, property.IsOptional) { Rules = rules };
}

public static class PropertyRequestExpandedExtensions
{
	public static PropertyRequest ToRequest(this PropertyRequestExpanded property)
		=> new (property.Type, property.IsOptional) { Rules = property.Rules };
}