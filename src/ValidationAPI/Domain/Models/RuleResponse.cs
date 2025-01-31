using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Models;

public record RuleResponse(string Name, RuleType Type, object Value, string ErrorMessage);

public static partial class RuleExtensions
{
	public static RuleResponse ToResponse(this Rule rule)
	{
		object value = rule.ValueType switch
		{
			RuleValueType.Int   => long.Parse(rule.Value),
			RuleValueType.Float => double.Parse(rule.Value),
			_ => rule.RawValue ?? rule.Value
		};
		
		return new RuleResponse(rule.Name, rule.Type, value, rule.ErrorMessage ?? string.Empty);
	}
}