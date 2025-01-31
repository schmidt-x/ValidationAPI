using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Rules.Queries.GetRules;

public record GetRulesQuery(string? Endpoint, int PageNumber, int PageSize, RuleOrder? OrderBy, bool Desc);

public class GetRulesQueryHandler : RequestHandlerBase
{
	private readonly IValidator<GetRulesQuery> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;

	public GetRulesQueryHandler(IValidator<GetRulesQuery> validator, IUser user, IRepositoryContext db)
	{
		_validator = validator;
		_user = user;
		_db = db;
	}
	
	public async Task<Result<PaginatedList<RuleExpandedResponse>>> Handle(GetRulesQuery query, CancellationToken ct)
	{
		if (!_validator.Validate(query).IsValid)
		{
			return new NotFoundException();
		}
		
		var userId = _user.Id();
		var offset = (query.PageNumber - 1) * query.PageSize;
		List<RuleExpandedResponse> items;
		int count;
		
		
		if (string.IsNullOrEmpty(query.Endpoint))
		{
			count = await _db.Rules.CountAsync(userId, ct);
			
			if (query.PageNumber < 1 || query.PageSize < 1)
			{
				return PaginatedList([], count, query.PageNumber, query.PageSize);
			}
			
			items = await _db.Rules.GetAllExpandedResponsesAsync(userId, query.PageSize, offset, query.OrderBy, query.Desc, ct);
			return PaginatedList(items, count, query.PageNumber, query.PageSize);
		}
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(query.Endpoint, userId, ct);
		if (!endpointId.HasValue)
		{
			return new NotFoundException();
		}
		
		count = await _db.Rules.CountAsync(endpointId.Value, ct);
		
		if (query.PageNumber < 1 || query.PageSize < 1)
		{
			return PaginatedList([], count, query.PageNumber, query.PageSize);
		}
		
		items = await _db.Rules
			.GetAllExpandedResponsesByEndpointAsync(endpointId.Value, query.PageSize, offset, query.OrderBy, query.Desc, ct);
		
		return PaginatedList(items, count, query.PageNumber, query.PageSize);
	}
	
	private static PaginatedList<RuleExpandedResponse> PaginatedList(
		IReadOnlyCollection<RuleExpandedResponse> items,
		int count, 
		int pageNumber,
		int pageSize) => new(items, count, pageNumber, pageSize);
}