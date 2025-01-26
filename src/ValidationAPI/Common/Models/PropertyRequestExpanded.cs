using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Common.Models;

public record PropertyRequestExpanded(string Name, PropertyType Type, bool IsOptional)
{
	private readonly RuleRequest[] _rules = null!;
	public RuleRequest[] Rules
	{ get => _rules; init => _rules = value ?? []; } 
}