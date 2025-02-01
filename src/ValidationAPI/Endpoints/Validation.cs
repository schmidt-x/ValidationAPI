using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Features.Validation.Queries.ValidateRequest;
using ValidationAPI.Infra;
using ValidationAPI.Responses;

namespace ValidationAPI.Endpoints;

public class Validation : EndpointGroupBase
{
	private const string BaseAddress = "api/validate";
	
	public override void Map(WebApplication app)
	{
		var g = app.MapGroup(BaseAddress).WithTags("Validation");
		
		g.MapPost("{endpoint}", Validate)
			.WithSummary("Validates a request")
			.Produces<ValidationResult>()
			.Produces(StatusCodes.Status401Unauthorized)
			.Produces<FailResponse>(StatusCodes.Status422UnprocessableEntity)
			.RequireAuthorization();
	}
	
	public async Task<IResult> Validate(
		[FromRoute] string endpoint,
		[FromBody] Dictionary<string, JsonElement> body,
		ValidateRequestQueryHandler handler,
		CancellationToken ct)
	{
		// Should we care about UrlEncoded string, if it's validated for restricted chars anyway?
		// var decodedEndpoint = HttpUtility.UrlDecode(endpoint);
		
		Result<ValidationResult> result = await handler.Handle(new ValidateRequestQuery(endpoint, body), ct);
		
		return result.Match<IResult>(
			Results.Ok,
			ex => ex is NotFoundException
				? Results.NotFound()
				: Results.UnprocessableEntity(FailResponse.From(ex)));
	}
}