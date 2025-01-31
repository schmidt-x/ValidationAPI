using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Rules.Commands.CreateRules;
using ValidationAPI.Features.Rules.Commands.DeleteRule;
using ValidationAPI.Features.Rules.Commands.UpdateErrorMessage;
using ValidationAPI.Features.Rules.Commands.UpdateName;
using ValidationAPI.Features.Rules.Queries.GetRule;
using ValidationAPI.Features.Rules.Queries.GetRules;
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
		
		g.MapGet("{rule}", Get)
			.WithSummary("Returns a rule")
			.Produces<RuleExpandedResponse>()
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapGet("", GetAll)
			.WithSummary("Returns all rules (optionally scopes to a specific Endpoint)")
			.Produces<PaginatedList<RuleExpandedResponse>>()
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapDelete("{rule}", Delete)
			.WithSummary("Deletes a rule")
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapPatch("{rule}/name", UpdateName)
			.WithSummary("Renames a rule")
			.Produces<RuleExpandedResponse>()
			.Produces(StatusCodes.Status401Unauthorized)
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity)
			.DisableAntiforgery(); // TODO: remove
		
		g.MapPatch("{rule}/error-message", UpdateErrorMessage)
			.WithSummary("Updates rule's error message")
			.Produces<RuleExpandedResponse>()
			.Produces(StatusCodes.Status401Unauthorized)
			.DisableAntiforgery(); // TODO: remove
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
	
	public async Task<IResult> Get(
		[FromRoute] string rule, [FromQuery] string endpoint, GetRuleQueryHandler handler, CancellationToken ct)
	{
		var query = new GetRuleQuery(rule, endpoint);
		Result<RuleExpandedResponse> result = await handler.Handle(query, ct);
		
		return result.Match(Results.Ok, _ => Results.NotFound());
	}
	
	public async Task<IResult> GetAll(
		[AsParameters] GetRulesRequest request, GetRulesQueryHandler handler, CancellationToken ct)
	{
		var query = new GetRulesQuery(
			request.Endpoint,
			request.PageNumber ?? 1,
			request.PageSize ?? 50,
			request.OrderBy,
			request.Desc ?? false);
		
		Result<PaginatedList<RuleExpandedResponse>> result = await handler.Handle(query, ct);
		
		return result.Match(Results.Ok, _ => Results.NotFound());
	}
	
	public async Task<IResult> Delete(
		[FromRoute] string rule, [FromQuery] string endpoint, DeleteRuleCommandHandler handler, CancellationToken ct)
	{
		var command = new DeleteRuleCommand(rule, endpoint);
		Exception? ex = await handler.Handle(command, ct);
		
		return ex is null ? Results.NoContent() : Results.NotFound();
	}
	
	public async Task<IResult> UpdateName(
		[FromRoute] string rule,
		[FromQuery] string endpoint,
		[FromForm] string newName,
		UpdateNameCommandHandler handler,
		CancellationToken ct)
	{
		var command = new UpdateNameCommand(rule, endpoint, newName);
		Result<RuleExpandedResponse> result = await handler.Handle(command, ct);
		
		return result.Match(
			Results.Ok,
			ex => ex switch
			{
				NotFoundException => Results.NotFound(),
				_ => Results.UnprocessableEntity(FailResponse.From(ex))
			});
	}
	
	public async Task<IResult> UpdateErrorMessage(
		[FromRoute] string rule,
		[FromQuery] string endpoint,
		[FromForm] string errorMessage,
		UpdateErrorMessageCommandHandler handler,
		CancellationToken ct)
	{
		var command = new UpdateErrorMessageCommand(rule, endpoint, errorMessage);
		Result<RuleExpandedResponse> result = await handler.Handle(command, ct);
		
		return result.Match(Results.Ok, _ => Results.NotFound());
	}
}