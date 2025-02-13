using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using static ValidationAPI.Domain.Constants.ErrorCodes;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Common.Validators.RuleValidators;

public partial class RuleValidator
{
	[GeneratedRegex(@"^now([+-][\d.:]+)?$", RegexOptions.IgnoreCase)]
	private static partial Regex NowWithOffsetRegex();
	
	[GeneratedRegex(@"^{(\w+)([+-][\d.:]+)?}$")]
	private static partial Regex PropertyWithOffsetRegex();
	
	private List<Rule>? ValidateDateTime(string failureKey, string propertyName, RuleRequest[] rules)
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
					validatedRule = DateTimeValidateComparison<DateTime>(failureKey, propertyName, rule, PropertyType.DateTime);
					break;
				
				case RuleType.Between:
				case RuleType.Outside:
					validatedRule = DateTimeValidateRange(failureKey, rule, n => n);
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
	
	
	private ValidatedRule? DateTimeValidateComparison<T>(
		string failureKey, string propertyName, RuleRequest rule, PropertyType propertyType) where T : struct, IParsable<T>
	{
		string ruleValue;
		string? ruleRawValue = null;
		string? ruleExtraInfo = null;
		bool isRuleRelative = false;
		
		var typeName = propertyType.ToString();
		var value = rule.Value;
		
		if (value.ValueKind != JsonValueKind.String)
		{
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE, InvalidValueTypeMessage(rule.Name, "String", value.ValueKind.ToString()));
			return null;
		}
		
		var rawValue = value.GetString();
		if (string.IsNullOrWhiteSpace(rawValue))
		{
			Failures.AddErrorDetail(failureKey, EMPTY_RULE_VALUE, $"[{rule.Name}] Empty value.");
			return null;
		}
		
		if (rawValue.StartsWith('{'))
		{
			if (!rawValue.EndsWith('}'))
			{
				Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid ${typeName}.");
				return null;
			}
			if (rawValue.Length < 3)
			{
				Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty property name.");
				return null;
			}
			
			var propertyMatch = PropertyWithOffsetRegex().Match(rawValue);
			if (!propertyMatch.Success)
			{
				Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid property/offset.");
				return null;
			}
			
			var offsetGroup = propertyMatch.Groups[2];
			if (offsetGroup.Success)
			{
				if ((ruleExtraInfo = GetOffsetStringIfValid(offsetGroup)) is null)
				{
					Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset.");
					return null;
				}
			}
			
			var targetPropertyName = propertyMatch.Groups[1].Value;
			
			var errorDetail = ValidateTargetProperty(propertyName, targetPropertyName, propertyType, rule.Name);
			if (errorDetail != null)
			{
				Failures.AddErrorDetail(failureKey, errorDetail);
				return null;
			}
			
			if (!IsValid) return null;
			
			ruleValue = targetPropertyName;
			ruleRawValue = rawValue;
			isRuleRelative = true;
		}
		else if (rawValue.StartsWith("n", StringComparison.OrdinalIgnoreCase))
		{
			var nowMatch = NowWithOffsetRegex().Match(rawValue);
			if (!nowMatch.Success)
			{
				Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid {typeName}.");
				return null;
			}
			
			var offsetGroup = nowMatch.Groups[1];
			if (offsetGroup.Success)
			{
				if ((ruleExtraInfo = GetOffsetStringIfValid(offsetGroup)) is null)
				{
					Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset.");
					return null;
				}
				ruleRawValue = rawValue;
			}
			
			if (!IsValid) return null;
			ruleValue = RuleOption.Now;
		}
		else
		{
			if (!T.TryParse(rawValue, CultureInfo.InvariantCulture, out _))
			{
				Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid {typeName}.");
				return null;
			}
			
			if (!IsValid) return null;
			ruleValue = rawValue;
		}
		
		return new ValidatedRule(ruleValue, ruleRawValue, RuleValueType.String, ruleExtraInfo, isRuleRelative);
	}
	
	private ValidatedRule? DateTimeValidateRange<T>(string failureKey, RuleRequest rule, Func<DateTimeOffset, T> converter)
		where T : struct, IComparable<T>, IParsable<T>
	{
		var type = typeof(T);
		var typeName = type.Name;
		
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
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Array must contain 2 elements; got: {arrayLength}.");
			return null;
		}
		
		JsonElement lower, upper; 
		
		using (var enumerator = rule.Value.EnumerateArray())
		{
			enumerator.MoveNext(); lower = enumerator.Current;
			enumerator.MoveNext(); upper = enumerator.Current;
		}
		
		if (lower.ValueKind != JsonValueKind.String || lower.ValueKind != upper.ValueKind)
		{
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE,
				$"[{rule.Name}] Both values must be of the same type 'Json.String' representing valid {typeName} values.");
			return null;
		}
		
		var lBoundRaw = lower.GetString();
		if (string.IsNullOrWhiteSpace(lBoundRaw))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty value (lower bound).");
			return null;
		}
		
		var uBoundRaw = upper.GetString();
		if (string.IsNullOrWhiteSpace(uBoundRaw))
		{
			Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty value (upper bound).");
			return null;
		}
		
		bool isLowerBoundDynamic = false, isUpperBoundDynamic = false;
		T lBound, uBound;
		
		// I, most likely, should extract the following 2 'almost-identical' branches into a function...
		
		if (!lBoundRaw.StartsWith("n", StringComparison.OrdinalIgnoreCase))
		{
			if (!T.TryParse(lBoundRaw, CultureInfo.InvariantCulture, out lBound))
			{
				Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid {typeName} (lower bound).");
				return null;
			}
		}
		else
		{
			var nowMatch = NowWithOffsetRegex().Match(lBoundRaw);
			if (!nowMatch.Success)
			{
				Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid {typeName} (lower bound).");
				return null;
			}
			
			var lBoundDt = _now;
			isLowerBoundDynamic = true;
			
			var offsetGroup = nowMatch.Groups[1];
			if (offsetGroup.Success)
			{
				if (!TryGetOffset(offsetGroup, out var offset))
				{
					Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset (lower bound).");
					return null;
				}
				
				lBoundDt = type == typeof(DateOnly)
					? lBoundDt.AddDays(offset.Days) // ignore 'time' components
					: lBoundDt.Add(offset);
			}
			
			lBound = converter.Invoke(lBoundDt);
		}
		
		if (!uBoundRaw.StartsWith("n", StringComparison.OrdinalIgnoreCase))
		{
			if (!T.TryParse(uBoundRaw, CultureInfo.InvariantCulture, out uBound))
			{
				Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid {typeName} (upper bound)."); 
				return null;
			}
			
			if (isLowerBoundDynamic)
			{
				Failures.AddErrorDetail(
					failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Upper bound cannot be fixed value while lower bound is 'now'.");
				return null;
			}
		}
		else
		{
			var nowMatch = NowWithOffsetRegex().Match(uBoundRaw);
			if (!nowMatch.Success)
			{
				Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid {typeName} (upper bound).");
				return null;
			}
			
			var uBoundDt = _now;
			isUpperBoundDynamic = true;
			
			var offsetGroup = nowMatch.Groups[1];
			if (offsetGroup.Success)
			{
				if (!TryGetOffset(offsetGroup, out var offset))
				{
					Failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset (upper bound).");
					return null;
				}
			
				uBoundDt = type == typeof(DateOnly)
					? uBoundDt.AddDays(offset.Days) // ignore 'time' components
					: uBoundDt.Add(offset);
			}
			
			uBound = converter.Invoke(uBoundDt);
		}
		
		if (lBound.CompareTo(uBound) >= 0)
		{
			Failures.AddErrorDetail(
				failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Lower bound cannot be equal to or greater than Upper bound.");
			return null;
		}
		
		if (!IsValid) return null;
		
		string ruleValue = isLowerBoundDynamic ? RuleOption.Now + lBoundRaw[RuleOption.Now.Length..] : lBoundRaw;
		string ruleRawValue = value.GetRawText();
		string ruleExtraInfo = isUpperBoundDynamic ? RuleOption.Now + uBoundRaw[RuleOption.Now.Length..] : uBoundRaw;

		return new ValidatedRule(ruleValue, ruleRawValue, RuleValueType.Range, ruleExtraInfo, false);
	}
	
	private static string? GetOffsetStringIfValid(Group offsetGroup)
	{
		var rawOffset = offsetGroup.ValueSpan;
		bool isTrimmed;
		
		// ReSharper disable once AssignmentInConditionalExpression
		if (isTrimmed = rawOffset.StartsWith('+'))
		{
			rawOffset = rawOffset[1..];
		}
		
		return TimeSpan.TryParse(rawOffset, out var offset) && offset.TotalSeconds != 0
			? isTrimmed ? rawOffset.ToString() : offsetGroup.Value
			: null;
	}

	private static bool TryGetOffset(Group offsetGroup, out TimeSpan offset)
	{
		var rawOffset = offsetGroup.ValueSpan;
		if (rawOffset.StartsWith('+')) rawOffset = rawOffset[1..];
		return TimeSpan.TryParse(rawOffset, out offset) && offset.TotalSeconds != 0;
	}
}