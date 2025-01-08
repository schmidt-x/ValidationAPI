using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Serilog;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Data;
using ValidationAPI.Features.Auth.Services;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Auth.Commands.SigIn;

public record SignInCommand(string Login, string Password);

public class SignInCommandHandler : RequestHandlerBase
{
	private readonly IValidator<SignInCommand> _validator;
	private readonly IRepositoryContext _db;
	private readonly IPasswordHasher _passwordHasher;
	private readonly IAuthSchemeProvider _schemeProvider;
	private readonly ILogger _logger;
	
	public SignInCommandHandler(
		IValidator<SignInCommand> validator,
		IRepositoryContext db,
		IPasswordHasher passwordHasher,
		IAuthSchemeProvider schemeProvider, ILogger logger)
	{
		_validator = validator;
		_db = db;
		_passwordHasher = passwordHasher;
		_schemeProvider = schemeProvider;
		_logger = logger;
	}
	
	public async Task<Result<ClaimsPrincipal>> Handle(SignInCommand request, CancellationToken ct)
	{
		var validationResult = _validator.Validate(request);
		if (!validationResult.IsValid)
		{
			return new AuthException();
		}
		
		var user = await _db.Users.GetByEmailIfExistsAsync(request.Login, ct);
		
		if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
		{
			return new AuthException();
		}
		
		_logger.Information("[{UserId}] [{Username}] signed in.", user.Id, user.Username);
		
		Claim[] claims = [ new(ClaimTypes.NameIdentifier, user.Id.ToString())];
		var identity = new ClaimsIdentity(claims, _schemeProvider.Scheme);
		
		return new ClaimsPrincipal(identity);
	}
}