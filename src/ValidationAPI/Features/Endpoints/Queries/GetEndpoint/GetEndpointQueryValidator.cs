using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Endpoints.Queries.GetEndpoint;

public class GetEndpointQueryValidator : AbstractValidator<GetEndpointQuery>
{
	public GetEndpointQueryValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled);
	}
}