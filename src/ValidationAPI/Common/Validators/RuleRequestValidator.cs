using System;
using System.Collections.Generic;
using FluentValidation;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Common.Validators;

public class RuleRequestValidator
{
	private readonly HashSet<string> _ruleNames = new(StringComparer.OrdinalIgnoreCase);
	
	public void Validate<T>(string failureKey, RuleRequest[] rules, ValidationContext<T> context)
	{
		foreach (var rule in rules)
		{
			if (string.IsNullOrWhiteSpace(rule.Name))
			{
				context.AddFailure(failureKey, ErrorCodes.EMPTY_RULE_NAME, "Rule names must not be empty.");
				break;
			}
			
			// TODO: validate rule name's length?
			
			if (!_ruleNames.Add(rule.Name))
			{
				context.AddFailure(failureKey, ErrorCodes.DUPLICATE_RULE_NAME,
					$"Rule names must be unique per endpoint (case-insensitive). Specifically '{rule.Name}'.");
				break;
			}
		}
	}
}