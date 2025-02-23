using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Domain.Constants;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Common.Validators.RuleValidators;

public partial class RuleValidator
{
	[GeneratedRegex(@"^{(\w+)(\..+)?}$")]
	private static partial Regex GetPropertyAndOptionRegex();
	
	private List<Rule>? ValidateString(string failureKey, string propertyName, RuleRequest[] rules)
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
					validatedRule = StringValidateComparison(failureKey, propertyName, rule);
					break;
				
				case RuleType.Between:
				case RuleType.Outside:
					validatedRule = StringValidateRange(failureKey, rule);
					break;
				
				case RuleType.Regex:
					validatedRule = StringValidateRegex(failureKey, rule);
					break;
				
				case RuleType.Email:
					if (!IsValid) continue;
					validatedRule = new ValidatedRule(string.Empty, null, RuleValueType.String, null, false);
					break;
					
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
	
	
	private ValidatedRule? StringValidateComparison(string failureKey, string propertyName, RuleRequest rule)
	{
		string ruleValue;
		string? ruleRawValue = null;
		string? ruleExtraInfo = null;
		bool isRuleRelative = false;
		var ruleValueType = RuleValueType.String;
		
		var value = rule.Value;
		switch (value.ValueKind)
		{
			case JsonValueKind.Number:
				if (!value.TryGetInt32(out var val))
				{
					Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Value is not a valid Number (Int32).");
					return null;
				}
				
				if (!IsValid) return null;
				
				ruleValue = val.ToString();
				ruleExtraInfo = RuleExtraInfo.ByLength;
				ruleValueType = RuleValueType.Int;
				break;
			
			case JsonValueKind.String:
				ruleRawValue = value.GetString();
				
				if (string.IsNullOrWhiteSpace(ruleRawValue))
				{
					Failures.AddErrorDetail(failureKey, EMPTY_RULE_VALUE, $"[{rule.Name}] Value is required.");
					return null;
				}
				
				if (ruleRawValue.StartsWith('\\')) // escaped value. Remove '\' and save as is
				{
					if (ruleRawValue.Length < 2) // must be at least 2 characters
					{
						Failures.AddErrorDetail(failureKey, EMPTY_RULE_VALUE, $"[{rule.Name}] Empty value.");
						return null;
					}
					
					if (!IsValid) return null;
					ruleValue = ruleRawValue[1..];
					break;
				}
				
				if (ruleRawValue.StartsWith('{'))
				{
					if (!ruleRawValue.EndsWith('}'))
					{
						Failures.AddErrorDetail(
							failureKey, INVALID_RULE_VALUE,
							$"[{rule.Name}] Value missing closing brace '}}'. Consider prepending '\\' for the exact comparison.");
						return null;
					}
					if (ruleRawValue.Length < 3)
					{
						Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty property name.");
						return null;
					}
					
					// the value is enclosed with '{}' and contains at least 1 character inside it
					var propertyMatch = GetPropertyAndOptionRegex().Match(ruleRawValue);
					if (!propertyMatch.Success)
					{
						Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid property name.");
						return null;
					}
					
					if (propertyMatch.Groups[2].Success) // option is captured
					{
						var option = propertyMatch.Groups[2].Value.ToUpperInvariant();
						ruleExtraInfo = option switch
						{
							RuleOption.ByLengthNormalized => RuleExtraInfo.ByLength,
							RuleOption.CaseINormalizedPostfix => RuleExtraInfo.CaseI,
							_ => null
						};
						if (ruleExtraInfo is null) // invalid option
						{
							Failures.AddErrorDetail(
								failureKey, INVALID_RULE_VALUE,
								$"[{rule.Name}] Invalid rule-option. Allowed options for 'String' property: " +
								$"'{RuleOption.ByLength}', " +
								$"'{RuleOption.CaseIPostfix}'.");
							return null;
						}
					}
					
					var targetPropertyName = propertyMatch.Groups[1].Value;
					
					var errorDetail = ValidateTargetProperty(propertyName, targetPropertyName, PropertyType.String, rule.Name);
					if (errorDetail != null)
					{
						Failures.AddErrorDetail(failureKey, errorDetail);
						return null;
					}
					
					if (!IsValid) return null;
					
					ruleValue = targetPropertyName;
					isRuleRelative = true;
					break;
				}
				
				if (ruleRawValue.StartsWith(RuleOption.CaseIPrefix, StringComparison.OrdinalIgnoreCase))
				{
					if (ruleRawValue.Length == RuleOption.CaseIPrefix.Length)
					{
						Failures.AddErrorDetail(
							failureKey, INVALID_RULE_VALUE, 
							$"[{rule.Name}] No value provided after the case-insensitive option. Consider prepending '\\' for exact comparison.");
						return null;
					}
					
					if (!IsValid) return null;
					
					ruleValue = ruleRawValue[RuleOption.CaseIPrefix.Length..]; // TODO: to upper?
					ruleExtraInfo = RuleExtraInfo.CaseI;
					break;
				}
				
				if (!IsValid) return null;
				
				ruleValue = ruleRawValue;
				ruleRawValue = null;
				break;
			
			default:
				Failures.AddErrorDetail(
					failureKey, INVALID_RULE_VALUE, InvalidValueTypeMessage(rule.Name, "Number, String", value.ValueKind.ToString()));
				return null;
		}
		
		return new ValidatedRule(ruleValue, ruleRawValue, ruleValueType, ruleExtraInfo, isRuleRelative);
	}
	
	private ValidatedRule? StringValidateRange(string failureKey, RuleRequest rule)
	{
		var value = rule.Value;
		
		if (rule.Value.ValueKind != JsonValueKind.Array)
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
		
		JsonElement left, right;
		
		using (var enumerator = rule.Value.EnumerateArray())
		{
			enumerator.MoveNext(); left = enumerator.Current;
			enumerator.MoveNext(); right = enumerator.Current;
		}
		
		if (left.ValueKind != JsonValueKind.Number || left.ValueKind != right.ValueKind)
		{
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE,
				$"[{rule.Name}] Both values must be of the same type 'Json.Number' representing valid Int32 values.");
			return null;
		}
		
		if (!left.TryGetInt32(out var lNum))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid Int32 (lower bound).");
			return null;
		}
		
		if (!right.TryGetInt32(out var rNum))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid Int32 (upper bound).");
			return null;
		}
		
		if (lNum >= rNum)
		{
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Lower bound cannot be equal to or greater than Upper bound.");
			return null;
		}
		
		return IsValid
			? new ValidatedRule(lNum.ToString(), value.GetRawText(), RuleValueType.Range, rNum.ToString(), false)
			: null;
	}
	
	private ValidatedRule? StringValidateRegex(string failureKey, RuleRequest rule)
	{
		var value = rule.Value;
		
		if (value.ValueKind != JsonValueKind.String)
		{
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE, InvalidValueTypeMessage(rule.Name, "String", value.ValueKind.ToString()));
			return null;
		}
		
		var regExp = value.GetString();
		if (string.IsNullOrWhiteSpace(regExp))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty Regex expression.");
			return null;
		}
		
		try
		{
			_ = new Regex(regExp);
		}
		catch (Exception ex)
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] {ex.Message}.");
			return null;
		}
		
		return IsValid ? new ValidatedRule(regExp, null, RuleValueType.String, null, false) : null;
	}
	
	private static string InvalidValueTypeMessage(string ruleName, string expectedTypes, string actualType)
		=> $"[{ruleName}] Value must be one of the following types: {expectedTypes}; got: {actualType}.";
	
	private ErrorDetail? ValidateTargetProperty(string name, string targetName, PropertyType type, string ruleName)
	{
		if (name.Equals(targetName, StringComparison.Ordinal))
		{
			return new ErrorDetail(INVALID_RULE_VALUE, $"[{ruleName}] Rule must not reference its own property.");
		}
		
		if (!_properties.TryGetValue(targetName, out var targetProperty))
		{
			return new ErrorDetail(
				INVALID_RULE_VALUE, $"[{ruleName}] Target property '{targetName}' not found (case-sensitive).");
		}
		
		if (targetProperty.Type != type)
		{
			return new ErrorDetail(
				INVALID_RULE_VALUE, $"[{ruleName}] Target property '{targetName}' must be of the same type ({type}).");
		}
		
		if (targetProperty.IsOptional)
		{
			return new ErrorDetail(INVALID_RULE_VALUE, $"[{ruleName}] Target property '{targetName}' must not be optional.");
		}
		
		return null;
	}
}
