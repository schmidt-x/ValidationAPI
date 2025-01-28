using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Validation.Queries.ValidateRequest;

public class ValidateEndpointQueryValidator : AbstractValidator<ValidateRequestQuery>
{
	public ValidateEndpointQueryValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled);
	}
}