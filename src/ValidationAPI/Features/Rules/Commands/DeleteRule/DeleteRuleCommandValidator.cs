using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Rules.Commands.DeleteRule;

public class DeleteRuleCommandValidator : AbstractValidator<DeleteRuleCommand>
{
	public DeleteRuleCommandValidator()
	{
		RuleFor(x => x.Rule)
			.NotEmpty();
		
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
	}
}