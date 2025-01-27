using System;
using static ValidationAPI.Domain.Constants.ErrorCodes;
using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Endpoints.Commands.RenameEndpoint;

public class RenameEndpointCommandValidator : AbstractValidator<RenameEndpointCommand>
{
	public RenameEndpointCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled);
		
		RuleFor(x => x.NewName)
			.NotEmpty()
			.WithErrorCode(EMPTY_ENDPOINT_NAME)
			.WithMessage("Endpoint is required.")
		
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.WithErrorCode(INVALID_ENDPOINT_NAME)
			.WithMessage("Endpoint can only contain letters (a-z, A-Z), digits (0-9), hyphens (-), or periods.")
			.When(x => !string.IsNullOrEmpty(x.NewName), ApplyConditionTo.CurrentValidator)
		
			.NotEqual(x => x.Endpoint, StringComparer.OrdinalIgnoreCase)
			.WithErrorCode(DUPLICATE_ENDPOINT_NAME)
			.WithMessage("New name must not be the same as the original name (case-insensitive).")
			.When(x => !string.IsNullOrEmpty(x.NewName), ApplyConditionTo.CurrentValidator);
	}
}