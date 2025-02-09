using System;
using System.Collections.Generic;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Validation.Models;

namespace ValidationAPI.Features.Validation.Validators;

file delegate string? Validator(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule);

public static partial class PropertyValidator
{
	public static void ValidateDateTime(
		UnvalidatedProperty property,
		Rule[] rules,
		Dictionary<string, UnvalidatedProperty> properties,
		Dictionary<string, List<ErrorDetail>> failures,
		DateTimeOffset now)
	{
		var actual = (DateTimeOffset)property.Value;
		
		foreach (var rule in rules)
		{
			Validator validator = rule.Type switch
			{
				RuleType.Less        => DateTimeLess,
				RuleType.More        => DateTimeMore,
				RuleType.LessOrEqual => DateTimeLessOrEqual,
				RuleType.MoreOrEqual => DateTimeMoreOrEqual,
				RuleType.Equal       => DateTimeEqual,
				RuleType.NotEqual    => DateTimeNotEqual,
				RuleType.Between     => DateTimeBetween,
				RuleType.Outside     => DateTimeOutside,
				RuleType.Regex       => throw new NotImplementedException("unreachable"),
				RuleType.Email       => throw new NotImplementedException("unreachable"),
				_ => throw new ArgumentOutOfRangeException(nameof(rules))
			};
			
			var expected = rule.IsRelative ? (DateTimeOffset?)properties[rule.Value].Value : null;
			
			var errorMessage = validator.Invoke(actual, expected, now, rule);
			if (errorMessage != null)
			{
				failures.AddErrorDetail(property.Name, rule.Name, errorMessage);
			}
		}
	}
	
	private static string? DateTimeLess(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) < 0 ? null : DateTimeFormatMessage(actual, rule);
	
	private static string? DateTimeMore(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) > 0 ? null : DateTimeFormatMessage(actual, rule);
	
	private static string? DateTimeLessOrEqual(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) <= 0 ? null : DateTimeFormatMessage(actual, rule);
	
	private static string? DateTimeMoreOrEqual(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) >= 0 ? null : DateTimeFormatMessage(actual, rule);

	private static string? DateTimeEqual(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) == 0 ? null : DateTimeFormatMessage(actual, rule);
	
	private static string? DateTimeNotEqual(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) != 0 ? null : DateTimeFormatMessage(actual, rule);
	
	private static string? DateTimeBetween(DateTimeOffset actual, DateTimeOffset? _, DateTimeOffset now, Rule rule)
	{
		var lower = DateTimeExtractRange(rule.Value, now);
		var upper = DateTimeExtractRange(rule.ExtraInfo!, now);
		return actual >= lower && actual <= upper ? null : DateTimeFormatRangeMessage(actual, rule);
	}
	
	private static string? DateTimeOutside(DateTimeOffset actual, DateTimeOffset? _, DateTimeOffset now, Rule rule)
	{
		var lower = DateTimeExtractRange(rule.Value, now);
		var upper = DateTimeExtractRange(rule.ExtraInfo!, now);
		return actual < lower || actual > upper ? null : DateTimeFormatRangeMessage(actual, rule);
	}
	
	
	private static int DateTimeCompare(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
	{
		expected ??= rule.Value.StartsWith('n') ? now : DateTimeOffset.Parse(rule.Value);
		
		if (rule.ExtraInfo != null)
			expected = expected.Value.Add(TimeSpan.Parse(rule.ExtraInfo));
		
		return actual.CompareTo(expected.Value);
	}
	
	private static DateTimeOffset DateTimeExtractRange(string value, DateTimeOffset now)
	{
		if (!value.StartsWith('n')) return DateTimeOffset.Parse(value);
		var startIndex = RuleOption.Now.Length;
		if (startIndex == value.Length) return now;
		if (value[startIndex] == '+') startIndex++;
		return now.Add(TimeSpan.Parse(value.AsSpan(startIndex)));
	}
	
	private static string DateTimeFormatMessage(DateTimeOffset actual, Rule rule)
	{
		var expected = rule.IsRelative ? rule.Value : rule.RawValue ?? rule.Value;
		
		return rule.ErrorMessage?
			.Replace(MessagePlaceholders.Value, expected, StringComparison.OrdinalIgnoreCase)
			.Replace(MessagePlaceholders.ActualValue, actual.ToString(), StringComparison.OrdinalIgnoreCase)
			?? string.Empty;
	}
	
	private static string DateTimeFormatRangeMessage(DateTimeOffset actual, Rule rule)
	{
		return rule.ErrorMessage?
			.Replace(MessagePlaceholders.Value1, rule.Value, StringComparison.OrdinalIgnoreCase)
			.Replace(MessagePlaceholders.Value2, rule.ExtraInfo!, StringComparison.OrdinalIgnoreCase)
			.Replace(MessagePlaceholders.ActualValue, actual.ToString(), StringComparison.OrdinalIgnoreCase)
			?? string.Empty;
	}
}
