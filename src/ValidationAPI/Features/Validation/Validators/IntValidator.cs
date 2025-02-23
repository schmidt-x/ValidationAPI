using System;
using System.Collections.Generic;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Validation.Models;

namespace ValidationAPI.Features.Validation.Validators;

file delegate bool Validator(long actual, long expected, Rule rule);

public partial class PropertyValidator
{
	private static void ValidateInt(
		UnvalidatedProperty property,
		Rule[] rules,
		Dictionary<string, UnvalidatedProperty> properties,
		Dictionary<string, List<ErrorDetail>> failures,
		DateTimeOffset _)
	{
		var actual = (long)property.Value;
		
		foreach (var rule in rules)
		{
			Validator validator = rule.Type switch
			{
				RuleType.Less        => IntLess,
				RuleType.More        => IntMore,
				RuleType.LessOrEqual => IntLessOrEqual,
				RuleType.MoreOrEqual => IntMoreOrEqual,
				RuleType.Equal       => IntEqual,
				RuleType.NotEqual    => IntNotEqual,
				RuleType.Between     => IntBetween,
				RuleType.Outside     => IntOutside,
				RuleType.Regex => throw new Exception("Unreachable code"),
				RuleType.Email => throw new Exception("Unreachable code"),
				_ => throw new ArgumentOutOfRangeException(nameof(rules))
			};
			
			var expected = rule.IsRelative ? (long)properties[rule.Value].Value : long.Parse(rule.Value);
			
			bool isValid = validator.Invoke(actual, expected, rule);
			if (isValid) continue;
			
			failures.AddErrorDetail(property.Name, rule.Name, NumberFormatMessage(actual, rule));
		}
	}
	
	
	private static bool IntLess(long actual, long expected, Rule _) => actual < expected;
	
	private static bool IntMore(long actual, long expected, Rule _) => actual > expected;
	
	private static bool IntLessOrEqual(long actual, long expected, Rule _) => actual <= expected;
	
	private static bool IntMoreOrEqual(long actual, long expected, Rule _) => actual >= expected;
	
	private static bool IntEqual(long actual, long expected, Rule _) => actual == expected;
	
	private static bool IntNotEqual(long actual, long expected, Rule _) => actual != expected;
	
	private static bool IntBetween(long actual, long expected, Rule rule) => actual >= expected && actual <= long.Parse(rule.ExtraInfo!);
	
	private static bool IntOutside(long actual, long expected, Rule rule) => actual < expected || actual > long.Parse(rule.ExtraInfo!);
	
	
	private static string NumberFormatMessage<T>(T actual, Rule rule) where T : struct
	{
		if (rule.Type is RuleType.Between or RuleType.Outside)
		{
			return rule.ErrorMessage?
				.Replace(MessagePlaceholders.Value1, rule.Value, StringComparison.OrdinalIgnoreCase)
				.Replace(MessagePlaceholders.Value2, rule.ExtraInfo!, StringComparison.OrdinalIgnoreCase)
				.Replace(MessagePlaceholders.ActualValue, actual.ToString(), StringComparison.OrdinalIgnoreCase)
				?? string.Empty;
		}
		
		return rule.ErrorMessage?
			.Replace(MessagePlaceholders.Value, rule.Value, StringComparison.OrdinalIgnoreCase)
			.Replace(MessagePlaceholders.ActualValue, actual.ToString(), StringComparison.OrdinalIgnoreCase)
			?? string.Empty;
	}
}