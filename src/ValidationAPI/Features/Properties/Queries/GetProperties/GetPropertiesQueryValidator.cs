using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Properties.Queries.GetProperties;

public class GetPropertiesQueryValidator : AbstractValidator<GetPropertiesQuery>
{
	public GetPropertiesQueryValidator()
	{
		RuleFor(x => x.Endpoint)
			.Matches(RegexPatterns.Endpoint)
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
	}
}