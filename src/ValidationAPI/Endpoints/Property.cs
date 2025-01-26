using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Features.Properties.Commands.CreateProperty;
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
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity);
		
		g.MapGet("{property}", Get)
			.WithSummary("Returns a property (optionally includes Rules)");
	}
	
	public static async Task<IResult> Create(
		CreatePropertyRequest request, CreatePropertyCommandHandler handler, CancellationToken ct)
	{
		var command = new CreatePropertyCommand(request.Endpoint, request.Property);
		var ex = await handler.Handle(command, ct);
		
		return ex is null
			? Results.Created($"{BaseAddress}/{request.Property.Name}", null)
			: Results.UnprocessableEntity(FailResponse.From(ex));
	}
	
	public static IResult Get(
		[FromRoute] string property,
		[FromQuery] bool includeRules = true)
	{
		System.Console.WriteLine(property);	
		throw new System.NotImplementedException();
	}
}
