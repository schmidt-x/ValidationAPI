using System.Text.RegularExpressions;
using FluentValidation;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Features.Endpoints.Commands.UpdateDescription;

public class UpdateDescriptionCommandValidator : AbstractValidator<UpdateDescriptionCommand>
{
	public UpdateDescriptionCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(RegexPatterns.Endpoint, RegexOptions.Compiled);
	}
}