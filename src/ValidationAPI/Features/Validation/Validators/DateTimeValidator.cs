﻿using System;
using System.Collections.Generic;
using System.Globalization;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Validation.Models;

namespace ValidationAPI.Features.Validation.Validators;

file delegate bool Validator(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule);

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
				RuleType.Regex       => throw new Exception("unreachable code"),
				RuleType.Email       => throw new Exception("unreachable code"),
				_ => throw new ArgumentOutOfRangeException(nameof(rules))
			};
			
			var expected = rule.IsRelative ? (DateTimeOffset?)properties[rule.Value].Value : null;
			
			bool isValid = validator.Invoke(actual, expected, now, rule);
			if (isValid) continue;
			
			failures.AddErrorDetail(property.Name, rule.Name, DateTimeFormatMessage(actual, rule));
		}
	}
	
	private static bool DateTimeLess(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) < 0;
	
	private static bool DateTimeMore(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) > 0;
	
	private static bool DateTimeLessOrEqual(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) <= 0;
	
	private static bool DateTimeMoreOrEqual(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) >= 0;

	private static bool DateTimeEqual(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) == 0;
	
	private static bool DateTimeNotEqual(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
		=> DateTimeCompare(actual, expected, now, rule) != 0;
	
	private static bool DateTimeBetween(DateTimeOffset actual, DateTimeOffset? _, DateTimeOffset now, Rule rule)
	{
		var lower = DateTimeExtractRange(rule.Value, now, DateTimeConverter);
		var upper = DateTimeExtractRange(rule.ExtraInfo!, now, DateTimeConverter);
		return actual >= lower && actual <= upper;
	}
	
	private static bool DateTimeOutside(DateTimeOffset actual, DateTimeOffset? _, DateTimeOffset now, Rule rule)
	{
		var lower = DateTimeExtractRange(rule.Value, now, DateTimeConverter);
		var upper = DateTimeExtractRange(rule.ExtraInfo!, now, DateTimeConverter);
		return actual < lower || actual > upper;
	}
	
	
	private static DateTimeOffset DateTimeConverter(DateTimeOffset dt) => dt;
	
	private static int DateTimeCompare(DateTimeOffset actual, DateTimeOffset? expected, DateTimeOffset now, Rule rule)
	{
		expected ??= rule.Value.StartsWith('n') ? now : DateTimeOffset.Parse(rule.Value);
		
		if (rule.ExtraInfo != null)
			expected = expected.Value.Add(TimeSpan.Parse(rule.ExtraInfo));
		
		return actual.CompareTo(expected.Value);
	}
	
	private static T DateTimeExtractRange<T>(string value, DateTimeOffset now, Func<DateTimeOffset, T> converter)
		where T : struct, IParsable<T>
	{
		if (!value.StartsWith('n'))
		{
			return T.Parse(value, CultureInfo.InvariantCulture);
		}
		var startIndex = RuleOption.Now.Length;
		if (startIndex == value.Length)
		{
			return converter.Invoke(now);
		}
		if (value[startIndex] == '+') startIndex++;
		
		var offset = TimeSpan.Parse(value.AsSpan(startIndex));
		return converter.Invoke(now.Add(offset));
	}
	
	private static string DateTimeFormatMessage<T>(T actual, Rule rule, string? format = null) where T : struct, IFormattable
	{
		if (rule.Type is RuleType.Between or RuleType.Outside)
		{
			return rule.ErrorMessage?
				.Replace(MessagePlaceholders.Value1, rule.Value, StringComparison.OrdinalIgnoreCase)
				.Replace(MessagePlaceholders.Value2, rule.ExtraInfo!, StringComparison.OrdinalIgnoreCase)
				.Replace(MessagePlaceholders.ActualValue, actual.ToString(format, null), StringComparison.OrdinalIgnoreCase)
				?? string.Empty;
		}
		
		var expected = rule.IsRelative ? rule.Value : rule.RawValue ?? rule.Value;
		return rule.ErrorMessage?
			.Replace(MessagePlaceholders.Value, expected, StringComparison.OrdinalIgnoreCase)
			.Replace(MessagePlaceholders.ActualValue, actual.ToString(format, null), StringComparison.OrdinalIgnoreCase)
			?? string.Empty;
	}
}
