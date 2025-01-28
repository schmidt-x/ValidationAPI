using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Properties.Commands.DeleteProperty;

public class DeletePropertyCommandValidator : AbstractValidator<DeletePropertyCommand>
{
	public DeletePropertyCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled)
			.When(x => !string.IsNullOrEmpty(x.Endpoint), ApplyConditionTo.CurrentValidator);
		
		RuleFor(x => x.Property)
			.NotEmpty()
			.Matches(RegexPatterns.Property, RegexOptions.Compiled)
			.When(x => !string.IsNullOrEmpty(x.Property), ApplyConditionTo.CurrentValidator);
	}
}