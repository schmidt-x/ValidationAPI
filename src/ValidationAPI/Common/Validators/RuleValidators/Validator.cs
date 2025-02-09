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

public partial class RuleValidator
{
	private readonly Dictionary<string, PropertyRequest> _properties;

	public Dictionary<string, List<ErrorDetail>> Failures { get; } = [];
	
	public bool IsValid => Failures.Count == 0;

	public RuleValidator(Dictionary<string, PropertyRequest> properties)
	{
		_properties = properties;
	}
	
	public List<Rule>? Validate(string failureKey, string propertyName, PropertyRequest property)
	{
		RuleValidatorDelegate ruleValidator = property.Type switch
		{
			PropertyType.String   => ValidateString,
			PropertyType.Int      => throw new NotImplementedException(),
			PropertyType.Float    => throw new NotImplementedException(),
			PropertyType.DateTime => ValidateDateTime,
			PropertyType.DateOnly => throw new NotImplementedException(),
			PropertyType.TimeOnly => throw new NotImplementedException(),
			_ => throw new ArgumentOutOfRangeException(nameof(property))
		};
		
		return ruleValidator.Invoke(failureKey, propertyName, property.Rules, _properties, Failures);
	}
}