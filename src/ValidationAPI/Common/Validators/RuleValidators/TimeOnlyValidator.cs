using System;
using System.Collections.Generic;
using System.Diagnostics;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Common.Validators.RuleValidators;

public partial class RuleValidator
{
	private List<Rule>? ValidateTimeOnly(string failureKey, string propertyName, RuleRequest[] rules)
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
					validatedRule = DateTimeValidateComparison<TimeOnly>(failureKey, propertyName, rule, PropertyType.TimeOnly);
					break;
				
				case RuleType.Between:
				case RuleType.Outside:
					validatedRule = DateTimeValidateRange(failureKey, rule, n => TimeOnly.FromDateTime(n.DateTime));
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
}