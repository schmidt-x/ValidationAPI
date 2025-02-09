using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	[GeneratedRegex(@"^now([+-][\d.:]{5,})?$", RegexOptions.IgnoreCase)]
	private static partial Regex NowWithOffsetRegex();
	
	[GeneratedRegex(@"^{(\w+)([+-][\d.:]{5,})?}$")]
	private static partial Regex PropertyWithOffsetRegex();
	
	private static List<Rule>? ValidateDateTime(
		string failureKey,
		string propertyName,
		RuleRequest[] rules,
		Dictionary<string, PropertyRequest> properties,
		Dictionary<string, List<ErrorDetail>> failures)
	{
		List<Rule> validatedRules = [];
		
		foreach (var rule in rules)
		{
			string ruleValue;
			string? ruleRawValue = null;
			string? ruleExtraInfo = null;
			bool isRuleRelative = false;
			var ruleValueType = RuleValueType.String;
			
			var value = rule.Value;
			switch (rule.Type)
			{
				case RuleType.Less:
				case RuleType.More:
				case RuleType.LessOrEqual:
				case RuleType.MoreOrEqual:
				case RuleType.Equal:
				case RuleType.NotEqual:
					if (value.ValueKind != JsonValueKind.String)
					{
						failures.AddErrorDetail(
							failureKey, INVALID_RULE_VALUE, InvalidValueTypeMessage(rule.Name, "String", value.ValueKind.ToString()));
						continue;
					}
					
					var rawDt = value.GetString();
					if (string.IsNullOrWhiteSpace(rawDt))
					{
						failures.AddErrorDetail(failureKey, EMPTY_RULE_VALUE, $"[{rule.Name}] Empty value.");
						continue;
					}
					
					if (rawDt.StartsWith('{'))
					{
						if (!rawDt.EndsWith('}'))
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateTime.");
							continue;
						}
						if (rawDt.Length < 3)
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty property name.");
							continue;
						}
						var relativeMatch = PropertyWithOffsetRegex().Match(rawDt);
						if (!relativeMatch.Success)
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid property/offset.");
							continue;
						}
						var offsetGroup = relativeMatch.Groups[2];
						if (offsetGroup.Success)
						{
							if ((ruleExtraInfo = GetOffsetStringIfValid(offsetGroup)) is null)
							{
								failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset.");
								continue;
							}
						}
						var targetPropertyName = relativeMatch.Groups[1].Value;
						var errorDetail = ValidateTargetProperty(
							propertyName, targetPropertyName, PropertyType.DateTime, rule.Name, properties);
						if (errorDetail != null)
						{
							failures.AddErrorDetail(failureKey, errorDetail);
							continue;
						}
						if (failures.Count != 0) continue;
						ruleValue = targetPropertyName;
						ruleRawValue = rawDt;
						isRuleRelative = true;
						break;
					}
					
					if (rawDt.StartsWith("n", StringComparison.OrdinalIgnoreCase))
					{
						var nowMatch = NowWithOffsetRegex().Match(rawDt);
						if (!nowMatch.Success)
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateTime.");
							continue;
						}
						var offsetGroup = nowMatch.Groups[1];
						if (offsetGroup.Success)
						{
							if ((ruleExtraInfo = GetOffsetStringIfValid(offsetGroup)) is null)
							{
								failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset.");
								continue;
							}
							ruleRawValue = rawDt;
						}
						if (failures.Count != 0) continue;
						ruleValue = RuleOption.Now;
						break;
					}
					
					// it's neither '{PropertyName[offset]}' nor 'now[offset]'
					
					if (!DateTimeOffset.TryParse(rawDt, out _))
					{
						failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateTime.");
						continue;
					}
					if (failures.Count != 0) continue;
					ruleValue = rawDt;
					break;
				
				case RuleType.Between:
				case RuleType.Outside:
					if (value.ValueKind != JsonValueKind.Array)
					{
						failures.AddErrorDetail(
							failureKey, INVALID_RULE_VALUE, InvalidValueTypeMessage(rule.Name, "Array", value.ValueKind.ToString()));
						continue;
					}
					
					var arrayLength = value.GetArrayLength();
					if (arrayLength != 2)
					{
						failures.AddErrorDetail(
							failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Array must contain 2 elements; got: {arrayLength}.");
						continue;
					}
					
					JsonElement lower; 
					JsonElement upper;
					
					using (var enumerator = rule.Value.EnumerateArray())
					{
						enumerator.MoveNext();
						lower = enumerator.Current;
						enumerator.MoveNext();
						upper = enumerator.Current;
					}
					
					if (lower.ValueKind != JsonValueKind.String || lower.ValueKind != upper.ValueKind)
					{
						failures.AddErrorDetail(
							failureKey, INVALID_RULE_VALUE, 
							$"[{rule.Name}] Both values must be of the same type 'Json.String' representing valid DateTime values.");
						continue;
					}
					
					var lBoundRaw = lower.GetString();
					if (string.IsNullOrWhiteSpace(lBoundRaw))
					{
						failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty value (lower bound).");
						continue;
					}
					
					var uBoundRaw = upper.GetString();
					if (string.IsNullOrWhiteSpace(uBoundRaw))
					{
						failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty value (upper bound).");
						continue;
					}
					
					var now = DateTimeOffset.UtcNow;
					bool isLowerBoundDynamic = false;
					bool isUpperBoundDynamic = false;
					
					DateTimeOffset lBound;
					DateTimeOffset uBound;
					
					if (!lBoundRaw.StartsWith("n", StringComparison.OrdinalIgnoreCase))
					{
						if (!DateTimeOffset.TryParse(lBoundRaw, out lBound))
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateTime (lower bound).");
							continue;
						}
					}
					else
					{
						var nowMatch = NowWithOffsetRegex().Match(lBoundRaw);
						if (!nowMatch.Success)
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateTime (lower bound).");
							continue;
						}
						lBound = now;
						isLowerBoundDynamic = true;
						var offsetGroup = nowMatch.Groups[1];
						if (offsetGroup.Success)
						{
							if (!TryGetOffset(offsetGroup, out var offset))
							{
								failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset (lower bound).");
								continue;
							}
							lBound = lBound.Add(offset);
						}
					}
					
					if (!uBoundRaw.StartsWith("n", StringComparison.OrdinalIgnoreCase))
					{
						if (!DateTimeOffset.TryParse(uBoundRaw, out uBound))
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateTime (upper bound).");
							continue;
						}
						if (isLowerBoundDynamic)
						{
							failures.AddErrorDetail(
								failureKey, INVALID_RULE_VALUE,
								$"[{rule.Name}] Upper bound cannot be fixed date while lower bound is 'now'.");
							continue;
						}
					}
					else
					{
						var nowMatch = NowWithOffsetRegex().Match(uBoundRaw);
						if (!nowMatch.Success)
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateTime (upper bound).");
							continue;
						}
						uBound = now;
						isUpperBoundDynamic = true;
						var offsetGroup = nowMatch.Groups[1];
						if (offsetGroup.Success)
						{
							if (!TryGetOffset(offsetGroup, out var offset))
							{
								failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset (upper bound).");
								continue;
							}
							uBound = uBound.Add(offset);
						}
					}
					
					if (lBound >= uBound)
					{
						failures.AddErrorDetail(
							failureKey, INVALID_RULE_VALUE,
							$"[{rule.Name}] Lower bound cannot be equal to or greater than upper bound.");
						continue;
					}
					if (failures.Count != 0) continue;
					
					ruleValue = isLowerBoundDynamic ? RuleOption.Now + lBoundRaw[RuleOption.Now.Length..] : lBoundRaw;
					ruleRawValue = value.GetRawText();
					ruleExtraInfo = isUpperBoundDynamic ? RuleOption.Now + uBoundRaw[RuleOption.Now.Length..] : uBoundRaw;
					ruleValueType = RuleValueType.Range;
					break;
				
				case RuleType.Regex:
				case RuleType.Email:
					failures.AddErrorDetail(failureKey, INVALID_RULE_TYPE, $"[{rule.Name}] Rule is not supported.");
					continue;
				
				default:
					throw new ArgumentOutOfRangeException(nameof(rules));
			}
			
			Debug.Assert(failures.Count == 0, "Call 'continue' if the rule has failed or 'failures.Count' != 0.");
			
			validatedRules.Add(new Rule
			{
				Name = rule.Name,
				NormalizedName = rule.Name.ToUpperInvariant(),
				Type = rule.Type,
				Value = ruleValue,
				RawValue = ruleRawValue,
				ValueType = ruleValueType,
				ExtraInfo = ruleExtraInfo,
				IsRelative = isRuleRelative,
				ErrorMessage = rule.ErrorMessage
			}); 
		}
		
		return failures.Count == 0 ? validatedRules : null;
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