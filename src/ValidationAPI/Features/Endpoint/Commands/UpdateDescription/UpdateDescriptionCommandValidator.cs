using System.Text.RegularExpressions;
using FluentValidation;

namespace ValidationAPI.Features.Endpoint.Commands.UpdateDescription;

public class UpdateDescriptionCommandValidator : AbstractValidator<UpdateDescriptionCommand>
{
	public UpdateDescriptionCommandValidator()
	{
		RuleFor(x => x.Endpoint)
			.NotEmpty()
			.Matches(@"^[a-zA-Z0-9\-.]+$", RegexOptions.Compiled);
	}
}