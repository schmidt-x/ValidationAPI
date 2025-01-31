using System;
using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Features.Rules.Commands.UpdateName;

public class UpdateNameCommandValidator : AbstractValidator<UpdateNameCommand>
{
	public UpdateNameCommandValidator()
	{
		RuleFor(x => x.Rule)
			.NotEmpty();
		
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.NewName)
			.NotEmpty()
			.WithErrorCode(EMPTY_RULE_NAME)
			.WithMessage("New rule name is required")
			
			.NotEqual(x => x.Rule, StringComparer.OrdinalIgnoreCase)
			.WithErrorCode(DUPLICATE_RULE_NAME)
			.WithMessage("New name must not be the same as the original name (case-insensitive).");
	}
}