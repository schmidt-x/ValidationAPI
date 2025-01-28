using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Properties.Queries.GetProperty;

public class GetPropertyQueryValidator : AbstractValidator<GetPropertyQuery>
{
	public GetPropertyQueryValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled);
			
		RuleFor(x => x.Property)
			.NotEmpty()
			.Matches(RegexPatterns.Property, RegexOptions.Compiled);
	}
}