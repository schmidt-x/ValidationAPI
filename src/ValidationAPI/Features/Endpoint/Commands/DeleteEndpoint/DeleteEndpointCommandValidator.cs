using System.Text.RegularExpressions;
using FluentValidation;

namespace ValidationAPI.Features.Endpoint.Commands.DeleteEndpoint;

public class DeleteEndpointCommandValidator : AbstractValidator<DeleteEndpointCommand>
{
	public DeleteEndpointCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(@"^[a-zA-Z0-9\-.]+$", RegexOptions.Compiled);
	}
}