using System.Text.RegularExpressions;
using static ValidationAPI.Domain.Constants.ErrorCodes;
using FluentValidation;
using ValidationAPI.Common.Validators;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Properties.Commands.CreateProperty;

public class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
	public CreatePropertyCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.WithErrorCode(EMPTY_ENDPOINT_NAME)
			.WithMessage("Endpoint is required.")
		
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.WithErrorCode(INVALID_ENDPOINT_NAME)
			.WithMessage("Invalid endpoint.")
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.Property)
			.NotNull()
			.WithErrorCode(EMPTY_PROPERTIES)
			.WithMessage("Property is required.");
			
		RuleFor(x => x.Property.Name)
			.NotEmpty()
			.WithErrorCode(EMPTY_PROPERTY_NAME)
			.WithMessage("Property name is required.")
			.When(x => x.Property != null, ApplyConditionTo.CurrentValidator)
			
			.Matches(RegexPatterns.Property, RegexOptions.Compiled)
			.WithErrorCode(INVALID_PROPERTY_NAME)
			.WithMessage(
				"Property name can only contain letters (a-z, A-Z), underscores (_), or digits (0-9) (except at the beginning).")
			.When(x => x.Property is { Name.Length: > 0 }, ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.Property.Rules)
			.Custom((rules, context) => new RuleRequestValidator().Validate("Property.Rules", rules, context))
			.When(x => x.Property is { Rules.Length: > 0 }, ApplyConditionTo.CurrentValidator);
	}
}
