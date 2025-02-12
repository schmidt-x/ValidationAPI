using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Validation.Models;

namespace ValidationAPI.Features.Validation.Validators;

file delegate bool Validator(string actual, string expected, Rule rule);

public static partial class PropertyValidator
{
	// Wrapper method to allow compatibility with delegates that require an additional 'DateTimeOffset' parameter
	private static void ValidateString(
		UnvalidatedProperty property,
		Rule[] rules,
		Dictionary<string, UnvalidatedProperty> properties,
		Dictionary<string, List<ErrorDetail>> failures,
		DateTimeOffset _) => ValidateString(property, rules, properties, failures);
	
	private static void ValidateString(
		UnvalidatedProperty property,
		Rule[] rules,
		Dictionary<string, UnvalidatedProperty> properties,
		Dictionary<string, List<ErrorDetail>> failures)
	{
		var actual = (string)property.Value;
		
		foreach (var rule in rules)
		{
			Validator validator = rule.Type switch
			{
				RuleType.Less        => StringLess,
				RuleType.More        => StringMore,
				RuleType.LessOrEqual => StringLessOrEqual,
				RuleType.MoreOrEqual => StringMoreOrEqual,
				RuleType.Equal       => StringEqual,
				RuleType.NotEqual    => StringNotEqual,
				RuleType.Between     => StringBetween,
				RuleType.Outside     => StringOutside,
				RuleType.Regex       => StringRegex,
				RuleType.Email       => StringEmail,
				_ => throw new ArgumentOutOfRangeException(nameof(rules))
			};
			
			var expected = rule.IsRelative ? (string)properties[rule.Value].Value : rule.Value;
			
			var isValid = validator.Invoke(actual, expected, rule);
			if (isValid) continue;
			
			failures.AddErrorDetail(property.Name, rule.Name, StringFormatMessage(actual, rule));
		}
	}
	
	
	private static bool StringLess(string actual, string expected, Rule rule) => StringCompare(actual, expected, rule) < 0;
	
	private static bool StringMore(string actual, string expected, Rule rule) => StringCompare(actual, expected, rule) > 0;
	
	private static bool StringLessOrEqual(string actual, string expected, Rule rule) => StringCompare(actual, expected, rule) <= 0;
	
	private static bool StringMoreOrEqual(string actual, string expected, Rule rule) => StringCompare(actual, expected, rule) >= 0;
	
	private static bool StringEqual(string actual, string expected, Rule rule) => StringIsEqual(actual, expected, rule);
	
	private static bool StringNotEqual(string actual, string expected, Rule rule) => !StringIsEqual(actual, expected, rule);
	
	private static bool StringBetween(string actual, string expected, Rule rule)
	{
		string expected2 = rule.ExtraInfo!;
		return actual.Length >= int.Parse(expected) && actual.Length <= int.Parse(expected2);
	}
	
	private static bool StringOutside(string actual, string expected, Rule rule)
	{
		string expected2 = rule.ExtraInfo!;
		return actual.Length < int.Parse(expected) || actual.Length > int.Parse(expected2);
	}
	
	private static bool StringRegex(string actual, string expected, Rule rule) => Regex.IsMatch(actual, expected);
	
	private static bool StringEmail(string actual, string expected, Rule rule) => actual.Contains('@'); // TODO
	
	
	private static int StringCompare(string actual, string expected, Rule rule)
	{
		return rule.ExtraInfo is null
			? string.CompareOrdinal(actual, expected)
			: rule.ExtraInfo switch
			{
				RuleExtraInfo.ByLength => actual.Length.CompareTo(rule.IsRelative ? expected.Length : int.Parse(expected)),
				RuleExtraInfo.CaseI => string.Compare(actual, expected, StringComparison.OrdinalIgnoreCase),
				_ => throw new NotImplementedException()
			};
	}
	
	private static bool StringIsEqual(string actual, string expected, Rule rule)
	{
		return rule.ExtraInfo is null
			? actual.Equals(expected, StringComparison.Ordinal)
			: rule.ExtraInfo switch
			{
				RuleExtraInfo.ByLength => actual.Length == (rule.IsRelative ? expected.Length : int.Parse(expected)),
				RuleExtraInfo.CaseI => actual.Equals(expected, StringComparison.OrdinalIgnoreCase),
				_ => throw new NotImplementedException()
			};
	}
	
	private static string StringFormatMessage(string actual, Rule rule)
	{
		if (rule.Type is RuleType.Between or RuleType.Outside)
		{
			return rule.ErrorMessage?
				.Replace(MessagePlaceholders.Value1, rule.Value, StringComparison.OrdinalIgnoreCase)
				.Replace(MessagePlaceholders.Value2, rule.ExtraInfo, StringComparison.OrdinalIgnoreCase)
				.Replace(MessagePlaceholders.ActualValue, actual.Length.ToString(), StringComparison.OrdinalIgnoreCase)
				?? string.Empty;
		}
		
		if (rule.ExtraInfo == RuleExtraInfo.ByLength)
			actual = actual.Length.ToString();
		
		return rule.ErrorMessage?
			.Replace(MessagePlaceholders.Value, rule.Value, StringComparison.OrdinalIgnoreCase)
			.Replace(MessagePlaceholders.ActualValue, actual, StringComparison.OrdinalIgnoreCase)
			?? string.Empty;
	}
}