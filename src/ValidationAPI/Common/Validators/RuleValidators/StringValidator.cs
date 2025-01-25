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

public static partial class RuleValidators
{
	[GeneratedRegex(@"^{(\w+)(\..+)?}$")]
	private static partial Regex GetPropertyAndOptionRegex();
	
	public static List<Rule>? ValidateString(
		string propertyName, RuleRequest[] rules,
		Dictionary<string, PropertyRequest> properties,
		Dictionary<string, List<ErrorDetail>> failures)
	{
		List<Rule> validatedRules = [];
		
		if (rules.Length == 0)
		{
			return failures.Count == 0 ? validatedRules : null;
		}
		
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
					switch (value.ValueKind)
					{
						case JsonValueKind.Number:
							if (!value.TryGetInt64(out var val))
							{
								failures.AddErrorDetail(
									propertyName, INVALID_RULE_VALUE, $"[{rule.Name}] Value is not a valid Number (Int64).");
								continue;
							}
							if (failures.Count != 0) continue;
							ruleValue = val.ToString();
							ruleExtraInfo = RuleExtraInfo.ByLength;
							ruleValueType = RuleValueType.Int;
							break;
						
						case JsonValueKind.String:
							ruleRawValue = value.GetString();
							if (string.IsNullOrEmpty(ruleRawValue))
							{
								failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE, $"[{rule.Name}] Value is required.");
								continue;
							}
							
							if (ruleRawValue.StartsWith('\\')) // escaped value. Remove '\' and save as is
							{
								if (ruleRawValue.Length < 2) // must be at least 2 characters
								{
									failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE, $"[{rule.Name}] Empty value.");
									continue;
								}
								if (failures.Count != 0) continue;
								ruleValue = ruleRawValue[1..];
								break;
							}
							
							if (ruleRawValue.StartsWith('{'))
							{
								if (!ruleRawValue.EndsWith('}'))
								{
									failures.AddErrorDetail(
										propertyName, INVALID_RULE_VALUE,
										$"[{rule.Name}] Missing closing bracket '}}'. Consider prepending '\\' for the exact comparison.");
									continue;
								}
								if (ruleRawValue.Length < 3)
								{
									failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE, $"[{rule.Name}] Empty property name.");
									continue;
								}
								// the value is enclosed with '{}' and contains at least 1 character inside it
								var match = GetPropertyAndOptionRegex().Match(ruleRawValue);
								if (!match.Success)
								{
									failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE, $"[{rule.Name}] Invalid property name.");
									continue;
								}
								if (match.Groups[2].Success) // option is captured
								{
									var option = match.Groups[2].Value.ToUpperInvariant();
									ruleExtraInfo = option switch
									{
										RuleOption.ByLengthNormalized => RuleExtraInfo.ByLength,
										RuleOption.CaseINormalizedPostfix => RuleExtraInfo.CaseI,
										_ => null
									};
									if (ruleExtraInfo is null) // invalid option
									{
										failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE,
											$"[{rule.Name}] Invalid rule-option. Allowed options for 'String' property: " +
											$"'{RuleOption.ByLength}', " +
											$"'{RuleOption.CaseIPostfix}'.");
										continue;
									}
								}
								
								var targetPropertyName = match.Groups[1].Value;
								if (targetPropertyName.Equals(propertyName, StringComparison.Ordinal))
								{
									failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE,
										$"[{rule.Name}] Rule must not reference its own property.");
									continue;
								}
								if (!properties.TryGetValue(targetPropertyName, out var targetProperty))
								{
									failures.AddErrorDetail(
										propertyName, INVALID_RULE_VALUE,
										$"[{rule.Name}] Target property '{targetPropertyName}' not found (case-sensitive).");
									continue;
								}
								if (targetProperty.Type != PropertyType.String)
								{
									failures.AddErrorDetail(
										propertyName, INVALID_RULE_VALUE,
										$"[{rule.Name}] Target property '{targetPropertyName}' must be of the same type (String).");
									continue;
								}
								if (targetProperty.IsOptional)
								{
									failures.AddErrorDetail(
										propertyName, INVALID_RULE_VALUE,
										$"[{rule.Name}] Target property '{targetPropertyName}' must not be optional.");
									continue;
								}
								if (failures.Count != 0) continue;
								ruleValue = targetPropertyName;
								isRuleRelative = true;
								break;
							}
							
							if (ruleRawValue.StartsWith(RuleOption.CaseIPrefix, StringComparison.OrdinalIgnoreCase))
							{
								if (ruleRawValue.Length == RuleOption.CaseIPrefix.Length)
								{
									failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE, 
										$"[{rule.Name}] No value provided after the case-insensitive option." +
										$" Consider prepending '\\' for exact comparison.");
									continue;
								}
								if (failures.Count != 0) continue;
								ruleValue = ruleRawValue[RuleOption.CaseIPrefix.Length..]; // TODO: to upper?
								ruleExtraInfo = RuleExtraInfo.CaseI;
								break;
							}
							
							if (failures.Count != 0) continue;
							ruleValue = ruleRawValue;
							ruleRawValue = null;
							break;
						
						default:
							failures.AddErrorDetail(
								propertyName, INVALID_RULE_VALUE,
								InvalidValueTypeMessage(rule.Name, "Number, String", value.ValueKind.ToString()));
							continue;
					}
					break;
				
				case RuleType.Between:
				case RuleType.Outside:
					failures.AddErrorDetail(propertyName, INVALID_RULE_TYPE,
						$"[{rule.Name}] Rule-type is not currently implemented.");
					continue;
				
				case RuleType.Regex:
					if (value.ValueKind != JsonValueKind.String)
					{
						failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE,
							InvalidValueTypeMessage(rule.Name, "String", value.ValueKind.ToString()));
						continue;
					}
					var regExp = value.GetString();
					if (string.IsNullOrWhiteSpace(regExp))
					{
						failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE, $"[{rule.Name}] Empty Regex expression.");
						continue;
					}
					try
					{
						_ = new Regex(regExp);
					}
					catch (Exception ex)
					{
						failures.AddErrorDetail(propertyName, INVALID_RULE_VALUE, $"[{rule.Name}] {ex.Message}.");
						continue;
					}
					if (failures.Count != 0) continue;
					ruleValue = regExp;
					break;
				
				case RuleType.Email:
					// rule.Value is ignored
					if (failures.Count != 0) continue;
					ruleValue = string.Empty;
					break;
					
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
	
	
	private static string InvalidValueTypeMessage(string ruleName, string expectedTypes, string actualType)
		=> $"[{ruleName}] Value must be one of the following types: {expectedTypes}; got: {actualType}.";
}
