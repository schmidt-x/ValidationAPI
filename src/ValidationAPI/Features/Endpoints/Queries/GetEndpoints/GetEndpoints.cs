using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Endpoints.Queries.GetEndpoints;

public record GetEndpointsQuery(int PageNumber, int PageSize, EndpointOrder? OrderBy, bool Desc);

public class GetEndpointsQueryHandler : RequestHandlerBase
{
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	
	public GetEndpointsQueryHandler(IUser user, IRepositoryContext db)
	{
		_user = user;
		_db = db;
	}
	
	public async Task<PaginatedList<EndpointResponse>> Handle(GetEndpointsQuery query, CancellationToken ct)
	{
		var userId = _user.Id();
		var offset = (query.PageNumber - 1) * query.PageSize;
		
		int count = await _db.Endpoints.CountAsync(userId, ct);
		
		if (query.PageNumber < 1 || query.PageSize < 1)
		{
			return PaginatedList([], count, query.PageNumber, query.PageSize);
		}
		
		var endpoints = await _db.Endpoints.GetAllResponsesAsync(userId, query.PageSize, offset, query.OrderBy, query.Desc, ct);
		
		return PaginatedList(endpoints, count, query.PageNumber, query.PageSize);
	}
	
	private static PaginatedList<EndpointResponse> PaginatedList(
		IReadOnlyCollection<EndpointResponse> items,
		int count,
		int pageNumber,
		int pageSize) => new(items, count, pageNumber, pageSize);
}