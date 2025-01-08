using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Features.Auth.Commands.SignUp;
using ValidationAPI.Infra;
using ValidationAPI.Responses;
using ValidationAPI.Extensions;

namespace ValidationAPI.Endpoints;

public class Auth : EndpointGroupBase
{
	public override void Map(WebApplication app)
	{
		var group = app
			.MapGroup("/api/auth")
			.WithTags("Auth")
			.DisableAntiforgery(); // TODO: remove
		
		group.MapPost("sign-up", SignUp)
			.WithSummary("Registers a new user")
			.Produces(StatusCodes.Status201Created)
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity);
		
	}
	
	public static async Task<IResult> SignUp(
		[FromForm] string email,
		[FromForm] string username,
		[FromForm] string password,
		SignUpCommandHandler handler,
		HttpResponse response,
		CancellationToken ct)
	{
		var result = await handler.Handle(new SignUpCommand(email, username, password), ct);
		
		if (result.IsError(out var ex))
		{
			return Results.UnprocessableEntity(ex is ValidationException vEx ? vEx.ToFailResponse() : throw ex);
		}
		
		response.StatusCode = StatusCodes.Status201Created;
		return Results.SignIn(result.Value, new AuthenticationProperties { IsPersistent = true });
	}
}