using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ValidationAPI.Features.Endpoint.Commands.CreateEndpoint;
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
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity);
		
		
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
}