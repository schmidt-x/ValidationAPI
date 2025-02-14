using System;
using System.Collections.Generic;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Validation.Models;

namespace ValidationAPI.Features.Validation.Validators;

file delegate bool Validator(DateOnly actual, DateOnly? expected, DateTimeOffset now, Rule rule);

public partial class PropertyValidator
{
	private static void ValidateDateOnly(
		UnvalidatedProperty property,
		Rule[] rules,
		Dictionary<string, UnvalidatedProperty> properties,
		Dictionary<string, List<ErrorDetail>> failures,
		DateTimeOffset now)
	{
		var actual = (DateOnly)property.Value;
		
		foreach (var rule in rules)
		{
			Validator validator = rule.Type switch
			{
				RuleType.Less        => DateOnlyLess,
				RuleType.More        => DateOnlyMore,
				RuleType.LessOrEqual => DateOnlyLessOrEqual,
				RuleType.MoreOrEqual => DateOnlyMoreOrEqual,
				RuleType.Equal       => DateOnlyEqual,
				RuleType.NotEqual    => DateOnlyNotEqual,
				RuleType.Between     => DateOnlyBetween,
				RuleType.Outside     => DateOnlyOutside,
				RuleType.Regex       => throw new NotImplementedException("unreachable"),
				RuleType.Email       => throw new NotImplementedException("unreachable"),
				_ => throw new ArgumentOutOfRangeException(nameof(rules))
			};
			
			var expected = rule.IsRelative ? (DateOnly?)properties[rule.Value].Value : null;
			
			bool isValid = validator.Invoke(actual, expected, now, rule);
			if (isValid) continue;
			
			failures.AddErrorDetail(property.Name, rule.Name, DateTimeFormatMessage(actual, rule));
		}
	}
	
	private static bool DateOnlyLess(DateOnly actual, DateOnly? expected, DateTimeOffset now, Rule rule)
		=> DateOnlyCompare(actual, expected, now, rule) < 0;
	
	private static bool DateOnlyMore(DateOnly actual, DateOnly? expected, DateTimeOffset now, Rule rule)
		=> DateOnlyCompare(actual, expected, now, rule) > 0;
	
	private static bool DateOnlyLessOrEqual(DateOnly actual, DateOnly? expected, DateTimeOffset now, Rule rule)
		=> DateOnlyCompare(actual, expected, now, rule) <= 0;
	
	private static bool DateOnlyMoreOrEqual(DateOnly actual, DateOnly? expected, DateTimeOffset now, Rule rule)
		=> DateOnlyCompare(actual, expected, now, rule) >= 0;
	
	private static bool DateOnlyEqual(DateOnly actual, DateOnly? expected, DateTimeOffset now, Rule rule)
		=> DateOnlyCompare(actual, expected, now, rule) == 0;
	
	private static bool DateOnlyNotEqual(DateOnly actual, DateOnly? expected, DateTimeOffset now, Rule rule)
		=> DateOnlyCompare(actual, expected, now, rule) != 0;
	
	private static bool DateOnlyBetween(DateOnly actual, DateOnly? _, DateTimeOffset now, Rule rule)
	{
		var lower = DateTimeExtractRange(rule.Value, now, DateOnlyConverter);
		var upper = DateTimeExtractRange(rule.ExtraInfo!, now, DateOnlyConverter);
		return actual >= lower && actual <= upper;
	}
	
	private static bool DateOnlyOutside(DateOnly actual, DateOnly? _, DateTimeOffset now, Rule rule)
	{
		var lower = DateTimeExtractRange(rule.Value, now, DateOnlyConverter);
		var upper = DateTimeExtractRange(rule.ExtraInfo!, now, DateOnlyConverter);
		return actual < lower || actual > upper;
	}
	
	
	private static DateOnly DateOnlyConverter(DateTimeOffset dt) => DateOnly.FromDateTime(dt.DateTime);
	
	private static int DateOnlyCompare(DateOnly actual, DateOnly? expected, DateTimeOffset now, Rule rule)
	{
		TimeSpan? offset = rule.ExtraInfo != null ? TimeSpan.Parse(rule.ExtraInfo) : null; 
		
		if (expected.HasValue)
		{
			if (offset.HasValue)
				expected = DateOnly.FromDateTime(expected.Value.ToDateTime(new TimeOnly(0)).Add(offset.Value));
		}
		else if (rule.Value.StartsWith('n'))
		{
			expected = offset.HasValue
				? DateOnly.FromDateTime(now.Add(offset.Value).DateTime)
				: DateOnly.FromDateTime(now.DateTime);
		}
		else
			expected = DateOnly.Parse(rule.Value);
		
		return actual.CompareTo(expected.Value);
	}
}