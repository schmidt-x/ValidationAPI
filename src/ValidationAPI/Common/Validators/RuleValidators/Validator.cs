using System;
using System.Collections.Generic;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Common.Validators.RuleValidators;

file delegate List<Rule>? RuleValidatorDelegate(
	string failureKey,
	string propertyName,
	RuleRequest[] rules,
	Dictionary<string, PropertyRequest> properties,
	Dictionary<string, List<ErrorDetail>> failures);

public static partial class RuleValidators
{
	public static List<Rule>? Validate(
		string failureKey,
		PropertyType propertyType,
		string propertyName,
		RuleRequest[] rules,
		Dictionary<string, PropertyRequest> properties,
		Dictionary<string, List<ErrorDetail>> failures)
	{
		RuleValidatorDelegate ruleValidator = propertyType switch
		{
			PropertyType.String   => ValidateString,
			PropertyType.Int      => throw new NotImplementedException(),
			PropertyType.Float    => throw new NotImplementedException(),
			PropertyType.DateTime => throw new NotImplementedException(),
			PropertyType.DateOnly => throw new NotImplementedException(),
			PropertyType.TimeOnly => throw new NotImplementedException(),
			_ => throw new ArgumentOutOfRangeException(nameof(propertyType))
		};
		
		return ruleValidator.Invoke(failureKey, propertyName, rules, properties, failures);
	}
}