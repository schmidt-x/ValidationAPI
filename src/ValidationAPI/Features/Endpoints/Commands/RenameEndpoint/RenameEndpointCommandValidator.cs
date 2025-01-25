using System;
using static ValidationAPI.Domain.Constants.ErrorCodes;
using System.Text.RegularExpressions;
using FluentValidation;

namespace ValidationAPI.Features.Endpoints.Commands.RenameEndpoint;

public class RenameEndpointCommandValidator : AbstractValidator<RenameEndpointCommand>
{
	public RenameEndpointCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(@"^[a-zA-Z0-9\-.]+$", RegexOptions.Compiled);
		
		RuleFor(x => x.NewName)
			.NotEmpty()
			.WithErrorCode(EMPTY_VALUE)
			.WithMessage("Endpoint is required.");
		
		RuleFor(x => x.NewName)
			.Matches(@"^[a-zA-Z0-9\-.]+$", RegexOptions.Compiled)
			.WithErrorCode(INVALID_CHAR_IN_VALUE)
			.WithMessage("Endpoint can only contain letters (a-z, A-Z), digits (0-9), hyphens (-), or periods.");
		
		RuleFor(x => x.NewName)
			.NotEqual(x => x.Endpoint, StringComparer.OrdinalIgnoreCase)
			.WithErrorCode(DUPLICATE_VALUE)
			.WithMessage("New name must not be the same as the original name (case-insensitive).");
	}
}