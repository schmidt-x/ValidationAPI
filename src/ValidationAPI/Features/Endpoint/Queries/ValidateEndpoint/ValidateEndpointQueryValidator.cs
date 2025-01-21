using System.Text.RegularExpressions;
using FluentValidation;

namespace ValidationAPI.Features.Endpoint.Queries.ValidateEndpoint;

public class ValidateEndpointQueryValidator : AbstractValidator<ValidateEndpointQuery>
{
	public ValidateEndpointQueryValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(@"^[a-zA-Z0-9\-.]+$", RegexOptions.Compiled);
	}
}