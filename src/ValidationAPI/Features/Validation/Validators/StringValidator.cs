using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Validation.Models;

namespace ValidationAPI.Features.Validation.Validators;

file delegate string? Validator(string actual, string expected, Rule rule);

public static partial class PropertyValidators
{
	public static void ValidateString(
		UnvalidatedProperty property,
		Rule[] rules,
		Dictionary<string, JsonElement> requestBody,
		Dictionary<string, List<ErrorDetail>> failures)
	{
		var requestValue = property.Value.GetString()!;
		
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
			
			string? errorMessage = validator.Invoke(
				requestValue, rule.IsRelative ? requestBody[rule.Value].GetString()! : rule.Value, rule);
			
			if (errorMessage != null)
			{
				failures.AddErrorDetail(property.Name, rule.Name, errorMessage);
			}
		}
	}
	
	
	private static string? StringLess(string actual, string expected, Rule rule)
		=> Compare(actual, expected, rule) < 0 ? null : StringFormatMessage(actual, rule);
	
	private static string? StringMore(string actual, string expected, Rule rule)
		=> Compare(actual, expected, rule) > 0 ? null : StringFormatMessage(actual, rule);
	
	private static string? StringLessOrEqual(string actual, string expected, Rule rule)
		=> Compare(actual, expected, rule) <= 0 ? null : StringFormatMessage(actual, rule);
	
	private static string? StringMoreOrEqual(string actual, string expected, Rule rule)
		=> Compare(actual, expected, rule) >= 0 ? null : StringFormatMessage(actual, rule);
	
	private static string? StringEqual(string actual, string expected, Rule rule)
		=> IsEqual(actual, expected, rule) ? null : StringFormatMessage(actual, rule);
	
	private static string? StringNotEqual(string actual, string expected, Rule rule)
		=> !IsEqual(actual, expected, rule) ? null : StringFormatMessage(actual, rule);
	
	private static string? StringBetween(string actual, string expected, Rule rule)
	{
		ExtractRange(rule, out var expected1, out var expected2);
		
		return actual.Length >= long.Parse(expected1) && actual.Length <= long.Parse(expected2)
			? null
			: StringFormatRangeMessage(actual, expected1.ToString(), expected2.ToString(), rule.ErrorMessage);
	}
	
	private static string? StringOutside(string actual, string expected, Rule rule)
	{
		ExtractRange(rule, out var expected1, out var expected2);
		
		return actual.Length < long.Parse(expected1) || actual.Length > long.Parse(expected2)
			? null
			: StringFormatRangeMessage(actual, expected1.ToString(), expected2.ToString(), rule.ErrorMessage);
	}
	
	private static string? StringRegex(string actual, string expected, Rule rule)
		=> new Regex(expected).IsMatch(actual) ? null : StringFormatMessage(actual, rule);
	
	private static string? StringEmail(string actual, string expected, Rule rule) // TODO
		=> actual.Contains('@') ? null : StringFormatMessage(actual, rule);
	
	
	private static long Compare(string actual, string expected, Rule rule)
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
	
	private static bool IsEqual(string actual, string expected, Rule rule)
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
	
	private static void ExtractRange(Rule rule, out ReadOnlySpan<char> expected1, out ReadOnlySpan<char> expected2)
	{
		var index = int.Parse(rule.ExtraInfo!);
		expected1 = rule.Value.AsSpan(0, index); expected2 = rule.Value.AsSpan(index+1);
	}
	
	private static string StringFormatMessage(string actual, Rule rule)
		=> rule.ErrorMessage?
				.Replace(MessagePlaceholders.Value, rule.Value, StringComparison.OrdinalIgnoreCase)
				.Replace(MessagePlaceholders.ActualValue, rule.ExtraInfo == RuleExtraInfo.ByLength
					? actual.Length.ToString() : actual, StringComparison.OrdinalIgnoreCase)
				?? string.Empty;
	
	private static string StringFormatRangeMessage(string actual, string expected1, string expected2, string? message)
		=> message?
				.Replace(MessagePlaceholders.Value1, expected1, StringComparison.OrdinalIgnoreCase)
				.Replace(MessagePlaceholders.Value2, expected2, StringComparison.OrdinalIgnoreCase)
				.Replace(MessagePlaceholders.ActualValue, actual.Length.ToString(), StringComparison.OrdinalIgnoreCase)
				?? string.Empty;
}