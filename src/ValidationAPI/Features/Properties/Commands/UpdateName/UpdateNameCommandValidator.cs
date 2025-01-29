using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Features.Properties.Commands.UpdateName;

public class UpdateNameCommandValidator : AbstractValidator<UpdateNameCommand>
{
	public UpdateNameCommandValidator()
	{
		RuleFor(x => x.Property)
			.NotEmpty()
			.Matches(RegexPatterns.Property, RegexOptions.Compiled)
			.When(x => !string.IsNullOrEmpty(x.Property), ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.NewName)
			.NotEmpty()
			.WithErrorCode(EMPTY_PROPERTY_NAME)
			.WithMessage("New property name is required.")
			
			.Matches(RegexPatterns.Property, RegexOptions.Compiled)
			.WithErrorCode(INVALID_PROPERTY_NAME)
			.WithMessage(
				"Property name can only contain letters (a-z, A-Z), underscores (_), or digits (0-9) (except at the beginning).")
			.When(x => !string.IsNullOrEmpty(x.NewName), ApplyConditionTo.CurrentValidator)
			
			.NotEqual(x => x.Property)
			.WithErrorCode(DUPLICATE_PROPERTY_NAME)
			.WithMessage("New name must not be the same as the original name (case-sensitive).");
	}
}