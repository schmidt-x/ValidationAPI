using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Models;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Properties.Queries.GetProperties;

public record GetPropertiesQuery(string? Endpoint, int PageNumber, int PageSize, PropertyOrder? OrderBy, bool Desc);

public class GetPropertiesQueryHandler : RequestHandlerBase
{
	private readonly IValidator<GetPropertiesQuery> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	
	public GetPropertiesQueryHandler(
		IValidator<GetPropertiesQuery> validator, IUser user, IRepositoryContext db)
	{
		_validator = validator;
		_user = user;
		_db = db;
	}
	
	public async Task<Result<PaginatedList<PropertyMinimalResponse>>> Handle(GetPropertiesQuery query, CancellationToken ct)
	{
		if (!_validator.Validate(query).IsValid)
		{
			return new NotFoundException();
		}
		
		var userId = _user.Id();
		int offset = (query.PageNumber - 1) * query.PageSize;
		List<PropertyMinimalResponse> items;
		int count;
		
		if (string.IsNullOrEmpty(query.Endpoint))
		{
			count = await _db.Properties.CountAsync(userId, ct);
			
			if (query.PageNumber < 1 || query.PageSize < 1)
			{
				return PaginatedList([], count, query.PageNumber, query.PageSize);
			}
			
			items = await _db.Properties
				.GetAllMinimalResponsesAsync(userId, query.PageSize, offset, query.OrderBy, query.Desc, ct);
			
			return PaginatedList(items, count, query.PageNumber, query.PageSize);
		}
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(query.Endpoint, userId, ct);
		if (!endpointId.HasValue)
		{
			return new NotFoundException();
		}
		
		count = await _db.Properties.CountAsync(endpointId.Value, ct);
		
		if (query.PageNumber < 1 || query.PageSize < 1)
		{
			return PaginatedList([], count, query.PageNumber, query.PageSize);
		}
		
		items = await _db.Properties
			.GetAllMinimalResponsesByEndpointAsync(endpointId.Value, query.PageSize, offset, query.OrderBy, query.Desc, ct);
		
		return PaginatedList(items, count, query.PageNumber, query.PageSize);
	}
	
	
	private static PaginatedList<PropertyMinimalResponse> PaginatedList(
		IReadOnlyCollection<PropertyMinimalResponse> items,
		int count,
		int pageNumber,
		int pageSize) => new(items, count, pageNumber, pageSize);
}