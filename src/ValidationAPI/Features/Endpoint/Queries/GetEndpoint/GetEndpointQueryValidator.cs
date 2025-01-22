using System.Text.RegularExpressions;
using FluentValidation;

namespace ValidationAPI.Features.Endpoint.Queries.GetEndpoint;

public class GetEndpointQueryValidator : AbstractValidator<GetEndpointQuery>
{
	public GetEndpointQueryValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(@"^[a-zA-Z0-9\-.]+$", RegexOptions.Compiled);
	}
}