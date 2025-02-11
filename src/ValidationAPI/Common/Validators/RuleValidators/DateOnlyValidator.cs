using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Common.Validators.RuleValidators;

public partial class RuleValidator
{
	private static List<Rule>? ValidateDateOnly( // TODO: refactor into a single, generic function along with DateTime and TimeOnly 
		string failureKey,
		string propertyName,
		RuleRequest[] rules,
		Dictionary<string, PropertyRequest> properties,
		Dictionary<string, List<ErrorDetail>> failures)
	{
		var now = DateTimeOffset.Now;
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
					
					var rawDateOnly = value.GetString();
					if (string.IsNullOrWhiteSpace(rawDateOnly))
					{
						failures.AddErrorDetail(failureKey, EMPTY_RULE_VALUE, $"[{rule.Name}] Empty value.");
						continue;
					}
					
					if (rawDateOnly.StartsWith('{'))
					{
						if (!rawDateOnly.EndsWith('}'))
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateOnly.");
							continue;
						}
						if (rawDateOnly.Length < 3)
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Empty property name.");
							continue;
						}
						var propertyMatch = PropertyWithOffsetRegex().Match(rawDateOnly);
						if (!propertyMatch.Success)
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid property/offset.");
							continue;
						}
						var offsetGroup = propertyMatch.Groups[2];
						if (offsetGroup.Success)
						{
							if ((ruleExtraInfo = GetOffsetStringIfValid(offsetGroup)) is null)
							{
								failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset.");
								continue;
							}
						}
						var targetPropertyName = propertyMatch.Groups[1].Value;
						var errorDetail = ValidateTargetProperty(propertyName, targetPropertyName, PropertyType.DateOnly, rule.Name, properties);
						if (errorDetail != null)
						{
							failures.AddErrorDetail(failureKey, errorDetail);
							continue;
						}
						if (failures.Count != 0) continue;
						ruleValue = targetPropertyName;
						ruleRawValue = rawDateOnly;
						isRuleRelative = true;
						break;
					}
					
					if (rawDateOnly.StartsWith("n", StringComparison.OrdinalIgnoreCase))
					{
						var nowMatch = NowWithOffsetRegex().Match(rawDateOnly);
						if (!nowMatch.Success)
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateOnly.");
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
							ruleRawValue = rawDateOnly;
						}
						if (failures.Count != 0) continue;
						ruleValue = RuleOption.Now;
						break;
					}
					
					if (!DateOnly.TryParse(rawDateOnly, out _))
					{
						failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateOnly.");
						continue;
					}
					
					if (failures.Count != 0) continue;
					ruleValue = rawDateOnly;
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
							$"[{rule.Name}] Both values must be of the same type 'Json.String' representing valid DateOnly values.");
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
					
					bool isLowerBoundDynamic = false;
					bool isUpperBoundDynamic = false;
					
					DateOnly lBound;
					DateOnly uBound;
					
					if (!lBoundRaw.StartsWith("n", StringComparison.OrdinalIgnoreCase))
					{
						if (!DateOnly.TryParse(lBoundRaw, CultureInfo.InvariantCulture, out lBound))
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateOnly (lower bound).");
							continue;
						}
					}
					else
					{
						var nowMatch = NowWithOffsetRegex().Match(lBoundRaw);
						if (!nowMatch.Success)
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateOnly (lower bound).");
							continue;
						}
						var lBoundDt = now;
						isLowerBoundDynamic = true;
						var offsetGroup = nowMatch.Groups[1];
						if (offsetGroup.Success)
						{
							if (!TryGetOffset(offsetGroup, out var offset))
							{
								failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset (lower bound).");
								continue;
							}
							lBoundDt = lBoundDt.AddDays(offset.Days); // ignore 'time' components
						}
						lBound = DateOnly.FromDateTime(lBoundDt.DateTime);
					}
					
					if (!uBoundRaw.StartsWith("n", StringComparison.OrdinalIgnoreCase))
					{
						if (!DateOnly.TryParse(uBoundRaw, out uBound))
						{
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateOnly (upper bound).");
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
							failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid DateOnly (upper bound).");
							continue;
						}
						var uBoundDt = now;
						isUpperBoundDynamic = true;
						var offsetGroup = nowMatch.Groups[1];
						if (offsetGroup.Success)
						{
							if (!TryGetOffset(offsetGroup, out var offset))
							{
								failures.AddErrorDetail(failureKey, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid offset (upper bound).");
								continue;
							}
							uBoundDt = uBoundDt.AddDays(offset.Days); // ignore 'time' components
						}
						uBound = DateOnly.FromDateTime(uBoundDt.DateTime);
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
				
				default: throw new ArgumentOutOfRangeException(nameof(rules));
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
}