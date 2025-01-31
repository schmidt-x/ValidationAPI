using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Rules.Commands.UpdateErrorMessage;

public class UpdateErrorMessageCommandValidator : AbstractValidator<UpdateErrorMessageCommand>
{
	public UpdateErrorMessageCommandValidator()
	{
		RuleFor(x => x.Rule)
			.NotEmpty();
		
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
	}
}