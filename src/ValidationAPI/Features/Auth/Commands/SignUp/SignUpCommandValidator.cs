using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.Options;
using ValidationAPI.Common.Options;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Features.Auth.Commands.SignUp;

public class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
	public SignUpCommandValidator(IOptions<AuthOptions> authOptions)
	{
		var auth = authOptions.Value;
		
		RuleFor(x => x.Email)
			.NotEmpty()
			.WithErrorCode(EMPTY_VALUE)
			.WithMessage("Email address is required.");
		
		RuleFor(x => x.Email)
			.EmailAddress()
			.WithErrorCode(INVALID_VALUE)
			.WithMessage("Invalid email address.");
		
		RuleFor(x => x.Username)
			.NotEmpty()
			.WithErrorCode(EMPTY_VALUE)
			.WithMessage("Username is required.");
		
		RuleFor(x => x.Username)
			.MinimumLength(auth.UsernameMinLength)
			.WithErrorCode(LENGTH_BELOW_MINIMUM)
			.WithMessage($"Username must contain at least {auth.UsernameMinLength} characters.");
		
		RuleFor(x => x.Username)
			.MaximumLength(auth.UsernameMaxLength)
			.WithErrorCode(LENGTH_ABOVE_MAXIMUM)
			.WithMessage($"Username's length must not exceed the limit of {auth.UsernameMaxLength} characters.");
		
		RuleFor(x => x.Username)
			.Matches(@"^[a-zA-Z\d_.]+$", RegexOptions.Compiled)
			.WithErrorCode(INVALID_CHAR_IN_VALUE)
			.WithMessage("Username can only contain lowercase (a-z), uppercase (A-Z), digits (0-9), underscores, or periods.");
		
		RuleFor(x => x.Password)
			.NotEmpty()
			.WithErrorCode(EMPTY_VALUE)
			.WithMessage("Password is required.");
		
		RuleFor(x => x.Password)
			.MinimumLength(auth.PasswordMinLength)
			.WithErrorCode(LENGTH_BELOW_MINIMUM)
			.WithMessage($"Password must contain at least {auth.PasswordMinLength} characters.");
		
		RuleFor(x => x.Password)
			.Matches(@"\d", RegexOptions.Compiled)
			.WithErrorCode(VALUE_MISSING_DIGIT)
			.WithMessage("Password must contain at least one digit (0-9).");
		
		RuleFor(x => x.Password)
			.Matches("[a-z]", RegexOptions.Compiled)
			.WithErrorCode(VALUE_MISSING_LOWERCASE)
			.WithMessage("Password must contain at least one lowercase character (a-z).");
		
		RuleFor(x => x.Password)
			.Matches("[A-Z]", RegexOptions.Compiled)
			.WithErrorCode(VALUE_MISSING_UPPERCASE)
			.WithMessage("Password must contain at least one uppercase character (A-Z).");
		
		RuleFor(x => x.Password)
			.Matches(@"[^a-zA-Z0-9\s]", RegexOptions.Compiled)
			.WithErrorCode(VALUE_MISSING_SYMBOL)
			.WithMessage("Password must contain at least one non-alphanumeric character.");
		
		RuleFor(x => x.Password)
			.Matches(@"^\S*$", RegexOptions.Compiled)
			.WithErrorCode(INVALID_CHAR_IN_VALUE)
			.WithMessage("Password must not contain any whitespaces.");
	}
}