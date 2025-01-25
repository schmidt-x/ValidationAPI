using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Features.Endpoints.Queries.ValidateEndpoint.Validators;

file delegate bool Validator(string left, string right, Rule rule);

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
				RuleType.Between     => throw new NotImplementedException(),
				RuleType.Outside     => throw new NotImplementedException(),
				RuleType.Regex       => StringRegex,
				RuleType.Email       => StringEmail,
				_ => throw new ArgumentOutOfRangeException(nameof(rules))
			};
			
			bool isValid = validator.Invoke(
				requestValue, rule.IsRelative ? requestBody[rule.Value].GetString()! : rule.Value, rule);
			
			if (isValid) continue;
			
			var errorMessage = rule.ErrorMessage?
				.Replace("{value}", rule.Value, StringComparison.OrdinalIgnoreCase)
				.Replace("{actualValue}", 
					rule.ExtraInfo == RuleExtraInfo.ByLength ? requestValue.Length.ToString() : requestValue,
					StringComparison.OrdinalIgnoreCase)
				?? string.Empty;
				
			failures.AddErrorDetail(property.Name, rule.Name, errorMessage);
		}
	}
	
	
	private static bool StringLess(string l, string r, Rule rule)
		=> Compare(l, r, rule) < 0;
	
	private static bool StringMore(string l, string r, Rule rule)
		=> Compare(l, r, rule) > 0;
	
	private static bool StringLessOrEqual(string l, string r, Rule rule)
		=> Compare(l, r, rule) <= 0;
	
	private static bool StringMoreOrEqual(string l, string r, Rule rule)
		=> Compare(l, r, rule) >= 0;
	
	private static bool StringEqual(string l, string r, Rule rule)
		=> IsEqual(l, r, rule);
	
	private static bool StringNotEqual(string l, string r, Rule rule)
		=> !IsEqual(l, r, rule);
	
	private static bool StringRegex(string l, string r, Rule rule)
		=> new Regex(r).IsMatch(l);
	
	private static bool StringEmail(string l, string _, Rule rule)
		=> throw new NotImplementedException();
	
	
	private static long Compare(string l, string r, Rule rule)
	{
		return rule.ExtraInfo is null
			? string.CompareOrdinal(l, r)
			: rule.ExtraInfo switch
			{
				RuleExtraInfo.ByLength => l.Length - (rule.IsRelative ? r.Length : long.Parse(r)),
				RuleExtraInfo.CaseI => string.Compare(l, r, StringComparison.OrdinalIgnoreCase),
				_ => throw new NotImplementedException()
			};
	}
	
	private static bool IsEqual(string l, string r, Rule rule)
	{
		return rule.ExtraInfo is null
			? l.Equals(r, StringComparison.Ordinal)
			: rule.ExtraInfo switch
			{
				RuleExtraInfo.ByLength => l.Length == (rule.IsRelative ? r.Length : long.Parse(r)),
				RuleExtraInfo.CaseI => l.Equals(r, StringComparison.OrdinalIgnoreCase),
				_ => throw new NotImplementedException()
			};
	}
}