using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Serilog;
using ValidationAPI.Common.Models;
using ValidationAPI.Data;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Features.Auth.Services;
using ValidationAPI.Features.Infra;
using ValidationException = ValidationAPI.Common.Exceptions.ValidationException;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Features.Auth.Commands.SignUp;

public record SignUpCommand(string Email, string Username, string Password);

public class SignUpCommandHandler : RequestHandlerBase
{
	private readonly IValidator<SignUpCommand> _validator;
	private readonly IRepositoryContext _db;
	private readonly IPasswordHasher _passwordHasher;
	private readonly ILogger _logger;
	private readonly IAuthSchemeProvider _schemeProvider;
	
	
	public SignUpCommandHandler(
		IValidator<SignUpCommand> validator,
		IRepositoryContext db,
		IPasswordHasher passwordHasher,
		ILogger logger,
		IAuthSchemeProvider schemeProvider)
	{
		_validator = validator;
		_db = db;
		_passwordHasher = passwordHasher;
		_logger = logger;
		_schemeProvider = schemeProvider;
	}
	
	public async Task<Result<ClaimsPrincipal>> Handle(SignUpCommand request, CancellationToken ct)
	{
		var validationResult = _validator.Validate(request);
		if (!validationResult.IsValid)
		{
			return new ValidationException(validationResult.Errors);
		}
		
		if (await _db.Users.EmailExistsAsync(request.Email, ct))
		{
			return new ValidationException(
				nameof(request.Email), DUPLICATE_VALUE, $"Email address '{request.Email}' is already taken.");
		}
		
		if (await _db.Users.UsernameExistsAsync(request.Username, ct))
		{
			return new ValidationException(
				nameof(request.Username), DUPLICATE_VALUE, $"Username '{request.Username}' is already taken.");
		}
		
		var timeNow = DateTimeOffset.UtcNow;
		
		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = request.Email,
			NormalizedEmail = request.Email.ToUpperInvariant(),
			Username = request.Username,
			NormalizedUsername = request.Username.ToUpperInvariant(),
			PasswordHash = _passwordHasher.Hash(request.Password),
			IsConfirmed = false,
			CreatedAt = timeNow,
			ModifiedAt = timeNow
		};
		
		try
		{
			await _db.Users.CreateUserAsync(user, ct);
		}
		catch (Exception ex)
		{
			_logger.Error("[{Action}] Failed to create a user: {ErrorMessage}", "SignUp", ex.Message);
			throw;
		}
		
		Claim[] claims = [ new(ClaimTypes.NameIdentifier, user.Id.ToString())];
		var identity = new ClaimsIdentity(claims, _schemeProvider.Scheme);
		
		_logger.Information("[{UserId}] [{Action}] User signed up.", user.Id, "SignUp");
		
		return new ClaimsPrincipal(identity);
	}
}