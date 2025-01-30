using System.Text.RegularExpressions;
using static ValidationAPI.Domain.Constants.ErrorCodes;
using FluentValidation;
using ValidationAPI.Common.Validators;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Rules.Commands.CreateRules;

public class CreateRulesCommandValidator : AbstractValidator<CreateRulesCommand>
{
	public CreateRulesCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.WithErrorCode(EMPTY_ENDPOINT_NAME)
			.WithErrorCode("Endpoint is required.")
			
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.WithErrorCode(INVALID_ENDPOINT_NAME)
			.WithMessage("Invalid endpoint.")
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.Property)
			.NotEmpty()
			.WithErrorCode(EMPTY_PROPERTY_NAME)
			.WithErrorCode("Property is required.")
			
			.Matches(RegexPatterns.Property, RegexOptions.Compiled)
			.WithErrorCode(INVALID_PROPERTY_NAME)
			.WithMessage("Invalid property.")
			.When(x => !string.IsNullOrEmpty(x.Property), ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.Rules)
			.NotEmpty()
			.WithErrorCode(EMPTY_RULES)
			.WithMessage("At least one rule is required.")
			
			.Custom((rules, context) => new RuleRequestValidator().Validate("Rules", rules, context))
			.When(x => x.Rules is { Length: > 0 }, ApplyConditionTo.CurrentValidator);
	}
}