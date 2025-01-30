using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Properties.Commands.CreateProperty;
using ValidationAPI.Features.Properties.Commands.DeleteProperty;
using ValidationAPI.Features.Properties.Commands.UpdateName;
using ValidationAPI.Features.Properties.Commands.UpdateOptionality;
using ValidationAPI.Features.Properties.Queries.GetProperties;
using ValidationAPI.Features.Properties.Queries.GetProperty;
using ValidationAPI.Infra;
using ValidationAPI.Requests;
using ValidationAPI.Responses;

namespace ValidationAPI.Endpoints;

public class Property : EndpointGroupBase
{
	private const string BaseAddress = "api/properties";
	
	public override void Map(WebApplication app)
	{
		var g = app
			.MapGroup(BaseAddress)
			.WithTags("Property")
			.RequireAuthorization();
		
		g.MapPost("", Create)
			.WithSummary("Appends a new property to an existing endpoint")
			.Produces(StatusCodes.Status201Created)
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity)
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapGet("{property}", Get)
			.WithSummary("Returns a property (optionally includes Rules)")
			.Produces<PropertyExpandedResponse>()
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapGet("", GetAll)
			.WithSummary("Returns all properties (optionally scopes to a specific Endpoint)")
			.Produces<PaginatedList<PropertyMinimalResponse>>()
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapDelete("{property}", Delete)
			.WithSummary("Deletes a property (including Rules)")
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status401Unauthorized);
		
		g.MapPatch("{property}/name", UpdateName)
			.WithSummary("Renames a property")
			.Produces<PropertyMinimalResponse>()
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity)
			.Produces(StatusCodes.Status401Unauthorized)
			.DisableAntiforgery(); // TODO: remove
		
		g.MapPatch("{property}/is-optional", UpdateOptionality)
			.WithSummary("Makes a property optional or required")
			.Produces<PropertyMinimalResponse>()
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity)
			.Produces(StatusCodes.Status401Unauthorized)
			.DisableAntiforgery(); // TODO: remove
	}
	
	public static async Task<IResult> Create(
		CreatePropertyRequest request, CreatePropertyCommandHandler handler, CancellationToken ct)
	{
		var command = new CreatePropertyCommand(request.Endpoint, request.Property);
		var ex = await handler.Handle(command, ct);
		
		return ex is null
			? Results.Created($"/{BaseAddress}/{request.Property.Name}?endpoint={request.Endpoint}", null)
			: Results.UnprocessableEntity(FailResponse.From(ex));
	}
	
	public static async Task<IResult> Get(
		[FromRoute] string property,
		[FromQuery] string endpoint,
		GetPropertyQueryHandler handler,
		CancellationToken ct,
		[FromQuery] bool includeRules = true)
	{
		var query = new GetPropertyQuery(property, endpoint, includeRules);
		Result<PropertyExpandedResponse> result = await handler.Handle(query, ct);
		
		return result.Match(Results.Ok, _ => Results.NotFound());
	}
	
	public static async Task<IResult> GetAll(
		[AsParameters] GetPropertiesRequest request, GetPropertiesQueryHandler handler, CancellationToken ct)
	{
		var query = new GetPropertiesQuery(
			request.Endpoint,
			request.PageNumber ?? 1,
			request.PageSize ?? 50,
			request.OrderBy,
			request.Desc ?? false);
		
		Result<PaginatedList<PropertyMinimalResponse>> res = await handler.Handle(query, ct);
		
		return res.Match(Results.Ok, _ => Results.NotFound());
	}
	
	public static async Task<IResult> Delete(
		[FromRoute] string property, [FromQuery] string endpoint, DeletePropertyCommandHandler handler, CancellationToken ct)
	{
		var command = new DeletePropertyCommand(property, endpoint);
		var ex = await handler.Handle(command, ct);
		
		return ex switch
		{
			null => Results.NoContent(),
			NotFoundException => Results.NotFound(),
			_ => Results.UnprocessableEntity(FailResponse.From(ex))
		};
	}
	
	public async Task<IResult> UpdateName(
		[AsParameters] PropertyUpdateNameRequest request, UpdateNameCommandHandler handler, CancellationToken ct)
	{
		var command = new UpdateNameCommand(request.Property, request.Endpoint, request.NewName);
		Result<PropertyMinimalResponse> result = await handler.Handle(command, ct);
		
		return result.Match(
			Results.Ok,
			ex => ex switch
			{
				NotFoundException => Results.NotFound(),
				_ => Results.UnprocessableEntity(FailResponse.From(ex))
			});
	}
	
	public async Task<IResult> UpdateOptionality(
		[AsParameters] PropertyUpdateOptionalityRequest request,
		UpdateOptionalityCommandHandler handler,
		CancellationToken ct)
	{
		var command = new UpdateOptionalityCommand(request.Property, request.Endpoint, request.IsOptional);
		Result<PropertyMinimalResponse> result = await handler.Handle(command, ct);
		
		return result.Match(
			Results.Ok,
			ex => ex switch
			{
				NotFoundException => Results.NotFound(),
				_ => Results.UnprocessableEntity(FailResponse.From(ex))
			});
	}
}
