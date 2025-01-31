using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Models;

public record RuleExpandedResponse(
	string Name, RuleType Type, object Value, string ErrorMessage, string Property, string Endpoint);

public static partial class RuleExtensions
{
	public static RuleExpandedResponse ToExpandedResponse(this Rule rule, string property, string endpoint)
	{
		object value = rule.ValueType switch
		{
			RuleValueType.Int   => long.Parse(rule.Value),
			RuleValueType.Float => double.Parse(rule.Value),
			_ => rule.RawValue ?? rule.Value
		};
		
		return new RuleExpandedResponse(rule.Name, rule.Type, value, rule.ErrorMessage ?? string.Empty, property, endpoint);
	}
}
