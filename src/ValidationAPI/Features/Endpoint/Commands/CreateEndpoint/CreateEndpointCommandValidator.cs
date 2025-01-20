using static ValidationAPI.Domain.Constants.ErrorCodes;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;

namespace ValidationAPI.Features.Endpoint.Commands.CreateEndpoint;

public partial class CreateEndpointCommandValidator : AbstractValidator<CreateEndpointCommand>
{
	public CreateEndpointCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.WithErrorCode(EMPTY_VALUE)
			.WithMessage("Endpoint is required.");
		
		RuleFor(x => x.Endpoint)
			.Matches(@"^[a-zA-Z0-9\-.]+$", RegexOptions.Compiled)
			.WithErrorCode(INVALID_CHAR_IN_VALUE)
			.WithMessage("Endpoint can only contain letters (a-z, A-Z), digits (0-9), hyphens (-), or periods.");
		
		RuleFor(x => x.Properties)
			.NotEmpty()
			.WithErrorCode(EMPTY_VALUE)
			.WithMessage("At least one property is required.")
			.Custom(Validator)
			.When(x => x.Properties is { Count: > 0 }, ApplyConditionTo.CurrentValidator);
	}
	
	
	private static void Validator(
		Dictionary<string, PropertyRequest> properties, ValidationContext<CreateEndpointCommand> context)
	{
		HashSet<string> ruleNames = [];
					
		foreach ((string propertyName, PropertyRequest property) in properties)
		{
			if (!PropertyNameRegex().IsMatch(propertyName))
			{
				context.AddFailure(propertyName, INVALID_CHAR_IN_VALUE,
					"Property name can only contain letters (a-z, A-Z), underscores (_), " +
					" or digits (0-9) (except at the beginning).");	
				continue;
			}
			
			// TODO: validate property length?
			
			foreach (var rule in property.Rules) // once any rule-error is detected, skip to the next property
			{
				if (string.IsNullOrWhiteSpace(rule.Name))
				{
					context.AddFailure(propertyName, EMPTY_RULE_NAME, "Rule names must not be empty.");
					break;
				}
				
				// TODO: validate rule name's length?
				
				if (!ruleNames.Add(rule.Name.ToUpperInvariant()))
				{
					context.AddFailure(propertyName, DUPLICATE_RULE_NAME,
						$"Rule names must be unique per endpoint (case-insensitive). Specifically '{rule.Name}'.");
					break;
				}
			}
		}
	}

  [GeneratedRegex(@"^[a-zA-Z_]\w*$")]
  private static partial Regex PropertyNameRegex();
}