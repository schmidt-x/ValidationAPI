using FluentValidation;
using Microsoft.Extensions.Options;
using ValidationAPI.Common.Options;

namespace ValidationAPI.Features.Auth.Commands.SigIn;

public class SignInCommandValidator : AbstractValidator<SignInCommand>
{
	public SignInCommandValidator(IOptions<AuthOptions> authOpts)
	{
		RuleFor(x => x.Login).NotEmpty().EmailAddress();
		
		RuleFor(x => x.Password)
			.NotEmpty()
			.MinimumLength(authOpts.Value.PasswordMinLength)
			.Matches("[a-z]")
			.Matches("[A-Z]")
			.Matches(@"\d")
			.Matches(@"[^a-zA-Z0-9\s]")
			.Matches(@"^\S*$");
	}
}