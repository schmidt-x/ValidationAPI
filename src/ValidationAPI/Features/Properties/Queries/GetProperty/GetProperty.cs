using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Properties.Queries.GetProperty;

public record GetPropertyQuery(string Property, string Endpoint, bool IncludeRules);

public class GetPropertyQueryHandler : RequestHandlerBase
{
	private readonly IValidator<GetPropertyQuery> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	
	public GetPropertyQueryHandler(IValidator<GetPropertyQuery> validator, IUser user, IRepositoryContext db)
	{
		_validator = validator;
		_user = user;
		_db = db;
	}
	
	public async Task<Result<PropertyExpandedResponse>> Handle(GetPropertyQuery query, CancellationToken ct)
	{
		if (!_validator.Validate(query).IsValid)
		{
			return new NotFoundException();
		}
		
		var userId = _user.Id();
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(query.Endpoint, userId, ct);
		if (!endpointId.HasValue)
		{
			return new NotFoundException();
		}
		
		var property = await _db.Properties
			.GetExpandedResponseIfExistsAsync(query.Property, endpointId.Value, query.IncludeRules, ct);
	
		return property is null ? new NotFoundException() : property;
	}
}
