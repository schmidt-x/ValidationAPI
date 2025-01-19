using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Features.Auth.Commands.SignUp;
using ValidationAPI.Features.Auth.Commands.SigIn;
using ValidationAPI.Infra;
using ValidationAPI.Responses;

namespace ValidationAPI.Endpoints;

public class Auth : EndpointGroupBase
{
	public override void Map(WebApplication app)
	{
		var group = app
			.MapGroup("api/auth")
			.WithTags("Auth")
			.DisableAntiforgery(); // TODO: remove
		
		group.MapPost("sign-up", SignUp)
			.WithSummary("Registers a new user")
			.Produces(StatusCodes.Status201Created)
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity);
		
		group.MapPost("sign-in", SignIn)
			.WithSummary("Logs in a user")
			.Produces(StatusCodes.Status204NoContent)
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity);
		
		group.MapPost("sign-out", SignOut)
			.WithSummary("Logs out a user")
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status401Unauthorized)
			.RequireAuthorization();
	}
	
	public static async Task<IResult> SignUp(
		[FromForm] string email,
		[FromForm] string username,
		[FromForm] string password,
		SignUpCommandHandler handler,
		HttpResponse response,
		CancellationToken ct)
	{
		Result<ClaimsPrincipal> result = await handler.Handle(new SignUpCommand(email, username, password), ct);
		
		if (result.IsError(out var ex))
		{
			return Results.UnprocessableEntity(FailResponse.From(ex));
		}
		
		response.StatusCode = StatusCodes.Status201Created;
		return Results.SignIn(result.Value, new AuthenticationProperties { IsPersistent = true });
	}
	
	public static async Task<IResult> SignIn(
		[FromForm] string login,
		[FromForm] string password,
		SignInCommandHandler handler,
		HttpResponse response,
		CancellationToken ct)
	{
		Result<ClaimsPrincipal> result = await handler.Handle(new SignInCommand(login, password), ct);
		
		if (result.IsError(out var ex))
		{
			return Results.UnprocessableEntity(FailResponse.From(ex));
		}
		
		response.StatusCode = StatusCodes.Status204NoContent;
		return Results.SignIn(result.Value, new AuthenticationProperties { IsPersistent = true });
	}
	
	public static IResult SignOut(
		HttpResponse response,
		Serilog.ILogger logger,
		IUser user)
	{
		// TODO: move into handler
		logger.Information("[{UserId}] [{Action}] User signed out.", user.Id(), "SignOut");
		
		response.StatusCode = StatusCodes.Status204NoContent;
		return Results.SignOut();
	}
}