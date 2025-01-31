using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Rules.Queries.GetRule;

public class GetRuleQueryValidator : AbstractValidator<GetRuleQuery>
{
	public GetRuleQueryValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.When(x => !string.IsNullOrEmpty(x.Endpoint));
		
		RuleFor(x => x.Rule)
			.NotEmpty();
	}
}