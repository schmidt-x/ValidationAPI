using System;
using System.Collections.Generic;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Validation.Models;

namespace ValidationAPI.Features.Validation.Validators;

file delegate bool Validator(TimeOnly actual, TimeOnly? expected, DateTimeOffset now, Rule rule);

public static partial class PropertyValidator
{
	private static void ValidateTimeOnly(
		UnvalidatedProperty property,
		Rule[] rules,
		Dictionary<string, UnvalidatedProperty> properties,
		Dictionary<string, List<ErrorDetail>> failures,
		DateTimeOffset now)
	{
		var actual = (TimeOnly)property.Value;
		
		foreach (var rule in rules)
		{
			Validator validator = rule.Type switch
			{
				RuleType.Less        => TimeOnlyLess,
				RuleType.More        => TimeOnlyMore,
				RuleType.LessOrEqual => TimeOnlyLessOrEqual,
				RuleType.MoreOrEqual => TimeOnlyMoreOrEqual,
				RuleType.Equal       => TimeOnlyEqual,
				RuleType.NotEqual    => TimeOnlyNotEqual,
				RuleType.Between     => TimeOnlyBetween,
				RuleType.Outside     => TimeOnlyOutside,
				RuleType.Regex       => throw new Exception("unreachable code"),
				RuleType.Email       => throw new Exception("unreachable code"),
				_ => throw new ArgumentOutOfRangeException(nameof(rules))
			};
			
			var expected = rule.IsRelative ? (TimeOnly?)properties[rule.Value].Value : null;
			
			bool isValid = validator.Invoke(actual, expected, now, rule);
			if (isValid) continue;
			
			failures.AddErrorDetail(property.Name, rule.Name, DateTimeFormatMessage(actual, rule, "HH:mm:ss"));
		}
	}
	
	private static bool TimeOnlyLess(TimeOnly actual, TimeOnly? expected, DateTimeOffset now, Rule rule)
		=> TimeOnlyCompare(actual, expected, now, rule) < 0;
	
	private static bool TimeOnlyMore(TimeOnly actual, TimeOnly? expected, DateTimeOffset now, Rule rule)
		=> TimeOnlyCompare(actual, expected, now, rule) > 0;
	
	private static bool TimeOnlyLessOrEqual(TimeOnly actual, TimeOnly? expected, DateTimeOffset now, Rule rule)
		=> TimeOnlyCompare(actual, expected, now, rule) <= 0;
	
	private static bool TimeOnlyMoreOrEqual(TimeOnly actual, TimeOnly? expected, DateTimeOffset now, Rule rule)
		=> TimeOnlyCompare(actual, expected, now, rule) >= 0;
	
	private static bool TimeOnlyEqual(TimeOnly actual, TimeOnly? expected, DateTimeOffset now, Rule rule)
		=> TimeOnlyCompare(actual, expected, now, rule) == 0;
	
	private static bool TimeOnlyNotEqual(TimeOnly actual, TimeOnly? expected, DateTimeOffset now, Rule rule)
		=> TimeOnlyCompare(actual, expected, now, rule) != 0;
	
	private static bool TimeOnlyBetween(TimeOnly actual, TimeOnly? _, DateTimeOffset now, Rule rule)
	{
		var lower = DateTimeExtractRange(rule.Value, now, TimeOnlyConverter);
		var upper = DateTimeExtractRange(rule.ExtraInfo!, now, TimeOnlyConverter);
		return actual >= lower && actual <= upper;
	}
	
	private static bool TimeOnlyOutside(TimeOnly actual, TimeOnly? _, DateTimeOffset now, Rule rule)
	{
		var lower = DateTimeExtractRange(rule.Value, now, TimeOnlyConverter);
		var upper = DateTimeExtractRange(rule.ExtraInfo!, now, TimeOnlyConverter);
		return actual < lower || actual > upper;
	}
	
	
	private static TimeOnly TimeOnlyConverter(DateTimeOffset dt) => TimeOnly.FromDateTime(dt.DateTime);
	
	private static int TimeOnlyCompare(TimeOnly actual, TimeOnly? expected, DateTimeOffset now, Rule rule)
	{
		TimeSpan? offset = rule.ExtraInfo != null ? TimeSpan.Parse(rule.ExtraInfo) : null;
		
		if (expected.HasValue)
		{
			if (offset.HasValue)
				expected = expected.Value.Add(offset.Value);
		}
		else if (rule.Value.StartsWith('n'))
		{
			expected = TimeOnly.FromDateTime((offset.HasValue ? now.Add(offset.Value) : now).DateTime);
		}
		else
			expected = TimeOnly.Parse(rule.Value);
		
		return actual.CompareTo(expected.Value);
	}
}