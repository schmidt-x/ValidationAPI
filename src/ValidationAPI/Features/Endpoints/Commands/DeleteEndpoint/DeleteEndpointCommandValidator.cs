using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Endpoints.Commands.DeleteEndpoint;

public class DeleteEndpointCommandValidator : AbstractValidator<DeleteEndpointCommand>
{
	public DeleteEndpointCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled);
	}
}