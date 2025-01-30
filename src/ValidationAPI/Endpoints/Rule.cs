using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Features.Rules.Commands.CreateRules;
using ValidationAPI.Infra;
using ValidationAPI.Requests;
using ValidationAPI.Responses;

namespace ValidationAPI.Endpoints;

public class Rule : EndpointGroupBase
{
	private const string BaseAddress = "api/rules";
	
	public override void Map(WebApplication app)
	{
		var g = app.MapGroup(BaseAddress)
			.WithTags("Rule")
			.RequireAuthorization();
		
		g.MapPost("", Create)
			.WithSummary("Appends new rules to an existing property")
			.Produces(StatusCodes.Status201Created)
			.Produces(StatusCodes.Status401Unauthorized)
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity);
	}
	
	public static async Task<IResult> Create(
		CreateRulesRequest request,
		CreateRulesCommandHandler handler,
		CancellationToken ct)
	{
		var command = new CreateRulesCommand(request.Endpoint, request.Property, request.Rules);
		Exception? ex = await handler.Handle(command, ct);
		
		return ex switch
		{
			null => Results.Created($"/{BaseAddress}/{request.Rules[0].Name}?endpoint={request.Endpoint}", null),
			_ => Results.UnprocessableEntity(FailResponse.From(ex))
		};
	}
	
	
}