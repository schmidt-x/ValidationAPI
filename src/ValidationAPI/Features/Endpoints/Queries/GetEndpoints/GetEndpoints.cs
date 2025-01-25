using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Endpoints.Queries.GetEndpoints;

public class GetEndpointsQueryHandler : RequestHandlerBase
{
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	
	public GetEndpointsQueryHandler(IUser user, IRepositoryContext db)
	{
		_user = user;
		_db = db;
	}
	
	public async Task<IReadOnlyCollection<EndpointResponse>> Handle(CancellationToken ct)
	{
		var endpoints = await _db.Endpoints.GetAllResponsesAsync(_user.Id(), ct);
		
		// TODO: return paginated and ordered list
		
		return endpoints;
	}
}