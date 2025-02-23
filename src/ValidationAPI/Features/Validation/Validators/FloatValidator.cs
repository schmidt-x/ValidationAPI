using System;
using System.Collections.Generic;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Validation.Models;

namespace ValidationAPI.Features.Validation.Validators;

file delegate bool Validator(double actual, double expected, Rule rule);

public partial class PropertyValidator
{
	private static void ValidateFloat(
		UnvalidatedProperty property,
		Rule[] rules,
		Dictionary<string, UnvalidatedProperty> properties,
		Dictionary<string, List<ErrorDetail>> failures,
		DateTimeOffset _)
	{
		var actual = (double)property.Value;
		
		foreach (var rule in rules)
		{
			Validator validator = rule.Type switch
			{
				RuleType.Less        => FloatLess,
				RuleType.More        => FloatMore,
				RuleType.LessOrEqual => FloatLessOrEqual,
				RuleType.MoreOrEqual => FloatMoreOrEqual,
				RuleType.Equal       => FloatEqual,
				RuleType.NotEqual    => FloatNotEqual,
				RuleType.Between     => FloatBetween,
				RuleType.Outside     => FloatOutside,
				RuleType.Regex => throw new Exception("Unreachable code"),
				RuleType.Email => throw new Exception("Unreachable code"),
				_ => throw new ArgumentOutOfRangeException(nameof(rules))
			};
			
			var expected = rule.IsRelative ? (double)properties[rule.Value].Value : double.Parse(rule.Value);
			
			bool isValid = validator.Invoke(actual, expected, rule);
			if (isValid) continue;
			
			failures.AddErrorDetail(property.Name, rule.Name, NumberFormatMessage(actual, rule));
		}
	}
	
	
	private static bool FloatLess(double actual, double expected, Rule _) => actual < expected;
	
	private static bool FloatMore(double actual, double expected, Rule _) => actual > expected;
	
	private static bool FloatLessOrEqual(double actual, double expected, Rule _) => actual <= expected;
	
	private static bool FloatMoreOrEqual(double actual, double expected, Rule _) => actual >= expected;
	
	private static bool FloatEqual(double actual, double expected, Rule _) => actual == expected; // TODO:
	
	private static bool FloatNotEqual(double actual, double expected, Rule _) => actual != expected; // TODO:
	
	private static bool FloatBetween(double actual, double expected, Rule rule) => actual >= expected && actual <= double.Parse(rule.ExtraInfo!);
	
	private static bool FloatOutside(double actual, double expected, Rule rule) => actual < expected || actual > double.Parse(rule.ExtraInfo!);
}