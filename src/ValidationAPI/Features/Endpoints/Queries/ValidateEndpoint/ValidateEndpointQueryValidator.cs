using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Endpoints.Queries.ValidateEndpoint;

public class ValidateEndpointQueryValidator : AbstractValidator<ValidateEndpointQuery>
{
	public ValidateEndpointQueryValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled);
	}
}