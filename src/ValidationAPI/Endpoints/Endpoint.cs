﻿using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Endpoints.Commands.CreateEndpoint;
using ValidationAPI.Features.Endpoints.Commands.RenameEndpoint;
using ValidationAPI.Features.Endpoints.Commands.DeleteEndpoint;
using ValidationAPI.Features.Endpoints.Commands.UpdateDescription;
using ValidationAPI.Features.Endpoints.Queries.GetEndpoint;
using ValidationAPI.Features.Endpoints.Queries.GetEndpoints;
using ValidationAPI.Features.Endpoints.Queries.ValidateEndpoint;
using ValidationAPI.Infra;
using ValidationAPI.Responses;
using ValidationAPI.Requests;

namespace ValidationAPI.Endpoints;

public class Endpoint : EndpointGroupBase
{
	private const string BaseAddress = "api/endpoints";
	
	public override void Map(WebApplication app)
	{
		var g = app
			.MapGroup(BaseAddress)
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
			.Produces<EndpointExpandedResponse>()
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapGet("", GetAll)
			.WithSummary("Returns all endpoints")
			.Produces<IReadOnlyCollection<EndpointResponse>>()
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapPatch("rename/{endpoint}", Rename)
			.WithSummary("Renames an endpoint")
			.Produces<EndpointResponse>()
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity)
			.Produces(StatusCodes.Status401Unauthorized)
			.DisableAntiforgery(); // TODO: remove
		
		g.MapPatch("update-description/{endpoint}", UpdateDescription)
			.WithSummary("Updates a description")
			.Produces<EndpointResponse>()
			.Produces(StatusCodes.Status401Unauthorized)
			.DisableAntiforgery(); // TODO: remove
		
		g.MapDelete("{endpoint}", Delete)
			.WithSummary("Deletes an endpoint (including Properties and Rules)")
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status401Unauthorized);
	}
	
	public static async Task<IResult> Create(
		CreateEndpointRequest request,
		CreateEndpointCommandHandler handler,
		CancellationToken ct)
	{
		var command = new CreateEndpointCommand(request.Endpoint, request.Description, request.Properties);
		var ex = await handler.Handle(command, ct);
		
		return ex is null
			? Results.Created($"{BaseAddress}/{request.Endpoint}", null)
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
		Result<EndpointExpandedResponse> result = await handler.Handle(query, ct);
		
		return result.Match<IResult>(Results.Ok, _ => Results.NotFound());
	}
	
	public static async Task<IResult> GetAll(
		GetEndpointsQueryHandler handler,
		CancellationToken ct)
	{
		var endpoints = await handler.Handle(ct);
		
		return Results.Ok(endpoints);
	}
	
	public static async Task<IResult> Rename(
		[FromRoute] string endpoint,
		[FromForm] string newName,
		RenameEndpointCommandHandler handler,
		CancellationToken ct)
	{
		Result<EndpointResponse> result = await handler.Handle(new RenameEndpointCommand(endpoint, newName), ct);
		
		return result.Match<IResult>(
			Results.Ok,
			ex => ex is NotFoundException
				? Results.NotFound()
				: Results.UnprocessableEntity(FailResponse.From(ex)));
	}
	
	public static async Task<IResult> UpdateDescription(
		[FromRoute] string endpoint,
		[FromForm] string? description,
		UpdateDescriptionCommandHandler handler,
		CancellationToken ct)
	{
		var command = new UpdateDescriptionCommand(endpoint, description);
		Result<EndpointResponse> result = await handler.Handle(command, ct);
		
		return result.Match(
			Results.Ok,
			ex => ex is NotFoundException
				? Results.NotFound()
				: Results.UnprocessableEntity(FailResponse.From(ex)));
	}
	
	public static async Task<IResult> Delete(
		[FromRoute] string endpoint,
		DeleteEndpointCommandHandler handler,
		CancellationToken ct)
	{
		var ex = await handler.Handle(new DeleteEndpointCommand(endpoint), ct);
		
		return ex is null
			? Results.NoContent()
			: Results.NotFound();
	}
}