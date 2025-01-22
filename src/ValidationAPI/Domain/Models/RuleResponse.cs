using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Models;

public record RuleResponse(string Name, RuleType Type, string Value, string ErrorMessage);

public static class RuleExtensions
{
	public static RuleResponse ToResponse(this Rule rule)
		=> new(rule.Name, rule.Type, rule.RawValue ?? rule.Value, rule.ErrorMessage ?? string.Empty);
}