using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Rules.Queries.GetRules;

public class GetRulesQueryValidator : AbstractValidator<GetRulesQuery>
{
	public GetRulesQueryValidator()
	{
		RuleFor(x => x.Endpoint)
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
	}
}