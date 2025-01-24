using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Endpoint.Queries.GetEndpoint;

public record GetEndpointQuery(string Endpoint, bool IncludePropertiesAndRules);

public class GetEndpointQueryHandler : RequestHandlerBase
{
	private readonly IValidator<GetEndpointQuery> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	
	public GetEndpointQueryHandler(
		IValidator<GetEndpointQuery> validator,
		IUser user,
		IRepositoryContext db)
	{
		_validator = validator;
		_user = user;
		_db = db;
	}
	
	public async Task<Result<EndpointExpandedResponse>> Handle(GetEndpointQuery query, CancellationToken ct)
	{
		if (!_validator.Validate(query).IsValid)
		{
			return new NotFoundException();
		}
		
		var userId = _user.Id();
		var endpoint = await _db.Endpoints.GetExpandedResponseIfExistsAsync(query.Endpoint, userId, query.IncludePropertiesAndRules, ct);
		
		// TODO: get Entity from db and convert it into Model here instead?
		// var responseProperties = endpoint.Properties
		// 	.Select(p =>
		// 	{
		// 		var responseRules = p.Rules.Select(r => r.ToResponse()).ToArray();
		// 		return p.ToResponse(responseRules);
		// 	}).ToArray();
		//
		// var responseProperty = endpoint.ToResponse(responseProperties);
		
		return endpoint is null ? new NotFoundException() : endpoint;
	}
}
