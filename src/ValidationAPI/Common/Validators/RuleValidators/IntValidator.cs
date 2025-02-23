using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Common.Validators.RuleValidators;

public partial class RuleValidator
{
	[GeneratedRegex(RegexPatterns.Property)]
	private static partial Regex PropertyNameRegex();
	
	private delegate bool Parser<T>(JsonElement element, out T value);
	
	private List<Rule>? ValidateInt(string failureKey, string propertyName, RuleRequest[] rules)
	{
		List<Rule> validatedRules = [];
		
		foreach (var rule in rules)
		{
			ValidatedRule? validatedRule;
			
			switch (rule.Type)
			{
				case RuleType.Less:
				case RuleType.More:
				case RuleType.LessOrEqual:
				case RuleType.MoreOrEqual:
				case RuleType.Equal:
				case RuleType.NotEqual:
					validatedRule = IntValidateComparison(failureKey, propertyName, rule);
					break;
				
				case RuleType.Between:
				case RuleType.Outside:
					validatedRule = NumberValidateRange(failureKey, PropertyType.Int, rule, (JsonElement e, out long v) => e.TryGetInt64(out v));
					break;
				
				case RuleType.Regex:
				case RuleType.Email:
					Failures.AddErrorDetail(failureKey, INVALID_RULE_TYPE, $"[{rule.Name}] Rule is not supported.");
					continue;
				
				default: throw new ArgumentOutOfRangeException(nameof(rules));
			}
			
			if (validatedRule is null) continue;
			
			Debug.Assert(IsValid);
			
			validatedRules.Add(new Rule
			{
				Name = rule.Name,
				NormalizedName = rule.Name.ToUpperInvariant(),
				Type = rule.Type,
				Value = validatedRule.Value,
				RawValue = validatedRule.RawValue,
				ValueType = validatedRule.ValueType,
				ExtraInfo = validatedRule.ExtraInfo,
				IsRelative = validatedRule.IsRelative,
				ErrorMessage = rule.ErrorMessage
			});
		}
		
		return IsValid ? validatedRules : null;
	}
	
	private ValidatedRule? IntValidateComparison(string failureKey, string propertyName, RuleRequest rule)
	{
		var value = rule.Value;
		switch (value.ValueKind)
		{
			case JsonValueKind.Number:
				if (!value.TryGetInt64(out var val))
				{
					Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid Int.");
					return null;
				}
				return IsValid ? new ValidatedRule(val.ToString(), null, RuleValueType.Int, null, false) : null;
			
			case JsonValueKind.String:
				return NumberValidateRelative(failureKey, propertyName, PropertyType.Int, rule);
			
			default:
				Failures.AddErrorDetail(
					failureKey, INVALID_RULE_VALUE, InvalidValueTypeMessage(rule.Name, "Number, String", value.ValueKind.ToString()));
				return null;
		}
	}
	
	private ValidatedRule? NumberValidateRelative(string failureKey, string propertyName, PropertyType propertyType, RuleRequest rule)
	{
		var ruleRawValue = rule.Value.GetString();
		if (string.IsNullOrWhiteSpace(ruleRawValue))
		{
			Failures.AddErrorDetail(failureKey, EMPTY_RULE_VALUE, $"[{rule.Name}] Value is required.");
			return null;
		}
		
		if (!ruleRawValue.StartsWith('{'))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid {propertyType}.");
			return null;
		}
		
		if (!ruleRawValue.EndsWith('}'))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Value missing closing brace '}}'.");
			return null;
		}
		
		if (ruleRawValue.Length < 3)
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty property name.");
			return null;
		}
		
		var targetPropertyName = ruleRawValue[1..^1];
		if (!PropertyNameRegex().IsMatch(targetPropertyName))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid property name.");
			return null;
		}
		
		var errorDetail = ValidateTargetProperty(propertyName, targetPropertyName, propertyType, rule.Name);
		if (errorDetail != null)
		{
			Failures.AddErrorDetail(failureKey, errorDetail);
			return null;
		}
		
		return IsValid
			? new ValidatedRule(targetPropertyName, ruleRawValue, RuleValueType.String, null, true)
			: null;
	}
	
	private ValidatedRule? NumberValidateRange<T>(string failureKey, PropertyType propertyType, RuleRequest rule, Parser<T> parser)
		where T : struct, INumber<T>
	{
		var value = rule.Value;
		if (value.ValueKind != JsonValueKind.Array)
		{
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE, InvalidValueTypeMessage(rule.Name, "Array", value.ValueKind.ToString()));
			return null;
		}
		
		var arrayLength = value.GetArrayLength();
		if (arrayLength != 2)
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Array must contain 2 elements; got: {arrayLength}.");
			return null;
		}
		
		JsonElement lower, upper;
		
		using (var enumerator = rule.Value.EnumerateArray())
		{
			enumerator.MoveNext(); lower = enumerator.Current;
			enumerator.MoveNext(); upper = enumerator.Current;
		}
		
		if (lower.ValueKind != JsonValueKind.Number || lower.ValueKind != upper.ValueKind)
		{
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE,
				$"[{rule.Name}] Both values must be of the same type 'Json.Number' representing valid {propertyType} values.");
			return null;
		}
		
		if (!parser.Invoke(lower, out T lBound))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid {propertyType} (lower bound).");
			return null;
		}
		
		if (!parser.Invoke(upper, out T uBound))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid {propertyType} (upper bound).");
			return null;
		}
		
		if (lBound >= uBound)
		{
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Lower bound cannot be equal to or greater than Upper bound.");
			return null;
		}
		
		if (!IsValid) return null;
			
		var ruleValue = lBound.ToString(null, CultureInfo.InvariantCulture);
		var ruleExtraInfo = uBound.ToString(null, CultureInfo.InvariantCulture);
		return new ValidatedRule(ruleValue, value.GetRawText(), RuleValueType.Range, ruleExtraInfo, false);
	}
}