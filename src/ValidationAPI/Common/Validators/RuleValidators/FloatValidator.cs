using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Common.Validators.RuleValidators;

public partial class RuleValidator
{
	private List<Rule>? ValidateFloat(string failureKey, string propertyName, RuleRequest[] rules)
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
					validatedRule = FloatValidateComparison(failureKey, propertyName, rule);
					break;
				
				case RuleType.Between:
				case RuleType.Outside:
					validatedRule = NumberValidateRange(failureKey, PropertyType.Float, rule, (JsonElement e, out double v) => e.TryGetDouble(out v));
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
	
	private ValidatedRule? FloatValidateComparison(string failureKey, string propertyName, RuleRequest rule)
	{
		var value = rule.Value;
		switch (value.ValueKind)
		{
			case JsonValueKind.Number:
				if (!value.TryGetDouble(out var val))
				{
					Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid Float.");
					return null;
				}
				
				return IsValid
					? new ValidatedRule(val.ToString(CultureInfo.InvariantCulture), null, RuleValueType.Float, null, false)
					: null;
			
			case JsonValueKind.String:
				return NumberValidateRelative(failureKey, propertyName, PropertyType.Float, rule);
			
			default:
				Failures.AddErrorDetail(
					failureKey, INVALID_RULE_VALUE, InvalidValueTypeMessage(rule.Name, "Number, String", value.ValueKind.ToString()));
				return null;
		}
	}
}