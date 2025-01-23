using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Endpoint.Commands.CreateEndpoint;
using ValidationAPI.Features.Endpoint.Commands.RenameEndpoint;
using ValidationAPI.Features.Endpoint.Queries.GetEndpoint;
using ValidationAPI.Features.Endpoint.Queries.ValidateEndpoint;
using ValidationAPI.Infra;
using ValidationAPI.Responses;
using ValidationAPI.Requests;

namespace ValidationAPI.Endpoints;

public class Endpoint : EndpointGroupBase
{
	public override void Map(WebApplication app)
	{
		var g = app
			.MapGroup("api/endpoints")
			.WithTags("Endpoint")
			.RequireAuthorization();
		
		g.MapPost("", Create)
			.WithSummary("Creates an endpoint for validation")
			.Produces(StatusCodes.Status201Created)
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity)
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapPost("validate/{endpoint}", Validate)
			.WithSummary("Validates a request")
			.Produces<ValidationResult>()
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity)
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapGet("{endpoint}", Get)
			.WithSummary("Returns an endpoint (optionally includes Properties and Rules)")
			.Produces<EndpointResponse>()
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapPut("{endpoint}", Rename)
			.WithSummary("Renames an endpoint")
			.Produces(StatusCodes.Status204NoContent)
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity)
			.Produces(StatusCodes.Status401Unauthorized)
			.DisableAntiforgery(); // TODO: remove
		
		
	}
	
	public static async Task<IResult> Create(
		EndpointRequest request,
		CreateEndpointCommandHandler handler,
		CancellationToken ct)
	{
		var command = new CreateEndpointCommand(request.Endpoint, request.Properties);
		var ex = await handler.Handle(command, ct);
		
		return ex is null
			? Results.Created()
			: Results.UnprocessableEntity(FailResponse.From(ex));
	}
	
	public static async Task<IResult> Validate(
		[FromRoute] string endpoint,
		[FromBody] Dictionary<string, JsonElement> body,
		ValidateEndpointQueryHandler handler,
		CancellationToken ct)
	{
		// Should we care about UrlEncoded string, if it's validated for restricted chars anyway?
		// var decodedEndpoint = HttpUtility.UrlDecode(endpoint);
		
		Result<ValidationResult> result = await handler.Handle(new ValidateEndpointQuery(endpoint, body), ct);
		
		return result.Match<IResult>(
			Results.Ok,
			ex => ex is NotFoundException
				? Results.NotFound()
				: Results.UnprocessableEntity(FailResponse.From(ex)));
	}
	
	public static async Task<IResult> Get(
		[FromRoute] string endpoint,
		GetEndpointQueryHandler handler,
		CancellationToken ct,
		[FromQuery] bool includePropertiesAndRules = true)
	{
		var query = new GetEndpointQuery(endpoint, includePropertiesAndRules);
		Result<EndpointResponse> result = await handler.Handle(query, ct);
		
		return result.Match<IResult>(Results.Ok, _ => Results.NotFound());
	}
	
	public static async Task<IResult> Rename(
		[FromRoute] string endpoint,
		[FromForm] string newName,
		RenameEndpointCommandHandler handler,
		CancellationToken ct)
	{
		Exception? ex = await handler.Handle(new RenameEndpointCommand(endpoint, newName), ct);
		
		return ex switch
		{
			null => Results.NoContent(),
			NotFoundException => Results.NotFound(),
			_ => Results.UnprocessableEntity(FailResponse.From(ex))
		};
	}
	
	
}