using System;
using System.Collections.Generic;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Common.Validators.RuleValidators;

file delegate List<Rule>? RuleValidatorDelegate(string failureKey, string propertyName, RuleRequest[] rules);

public partial class RuleValidator
{
	private record ValidatedRule(string Value, string? RawValue, RuleValueType ValueType, string? ExtraInfo, bool IsRelative);
	
	private readonly Dictionary<string, PropertyRequest> _properties;
	
	private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
	
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
			PropertyType.DateOnly => ValidateDateOnly,
			PropertyType.TimeOnly => throw new NotImplementedException(),
			_ => throw new ArgumentOutOfRangeException(nameof(property))
		};
		
		return ruleValidator.Invoke(failureKey, propertyName, property.Rules);
	}
}