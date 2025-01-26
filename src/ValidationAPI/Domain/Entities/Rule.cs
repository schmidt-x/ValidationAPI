using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Entities;

public class Rule
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public string NormalizedName { get; init; } = null!;
	public RuleType Type { get; init; }
	public string Value { get; init; } = null!;
	public RuleValueType ValueType { get; init; }
	public string? RawValue { get; init; }
	public string? ExtraInfo { get; init; }
	public bool IsRelative { get; init; }
	public string? ErrorMessage { get; init; }
	public int PropertyId { get; init; }
	public int EndpointId { get; init; }
}