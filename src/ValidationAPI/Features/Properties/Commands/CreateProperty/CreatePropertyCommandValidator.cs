using System.Collections.Generic;
using System.Text.RegularExpressions;
using static ValidationAPI.Domain.Constants.ErrorCodes;
using FluentValidation;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Extensions;

namespace ValidationAPI.Features.Properties.Commands.CreateProperty;

public class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
	public CreatePropertyCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.WithErrorCode(EMPTY_ENDPOINT_NAME)
			.WithMessage("Endpoint is required.")
		
			.Matches(@"^[a-zA-Z0-9\-.]+$", RegexOptions.Compiled)
			.WithErrorCode(INVALID_ENDPOINT_NAME)
			.WithMessage("Invalid endpoint.")
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.Property)
			.NotNull()
			.WithErrorCode(EMPTY_PROPERTY)
			.WithMessage("Property is required.");
			
		RuleFor(x => x.Property.Name)
			.NotEmpty()
			.WithErrorCode(EMPTY_PROPERTY_NAME)
			.WithMessage("Property name is required.")
			.When(x => x.Property != null, ApplyConditionTo.CurrentValidator)
			
			.Matches(@"^[a-zA-Z_]\w*$", RegexOptions.Compiled)
			.WithErrorCode(INVALID_PROPERTY_NAME)
			.WithMessage(
				"Property name can only contain letters (a-z, A-Z), underscores (_), or digits (0-9) (except at the beginning).")
			.When(x => x.Property is { Name.Length: > 0 }, ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.Property.Rules)
			.Custom(Validator)
			.When(x => x.Property is { Rules.Length: > 0 }, ApplyConditionTo.CurrentValidator);
	}
	
	private static void Validator(RuleRequest[] rules, ValidationContext<CreatePropertyCommand> context)
	{
		const string failureKey = "Property.Rules";
		HashSet<string> ruleNames = [];
		
		foreach (var rule in rules)
		{
			if (string.IsNullOrWhiteSpace(rule.Name))
			{
				context.AddFailure(failureKey, EMPTY_RULE_NAME, "Rule names must not be empty.");
				break;
			}
			
			// TODO: validate rule name's length?
			
			if (!ruleNames.Add(rule.Name.ToUpperInvariant()))
			{
				context.AddFailure(failureKey, DUPLICATE_RULE_NAME,
					$"Rule names must be unique per endpoint (case-insensitive). Specifically '{rule.Name}'.");
				break;
			}
		}
	}
}

