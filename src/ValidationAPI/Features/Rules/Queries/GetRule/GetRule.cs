using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Rules.Queries.GetRule;

public record GetRuleQuery(string Rule, string Endpoint);

public class GetRuleQueryHandler : RequestHandlerBase
{
	private readonly IValidator<GetRuleQuery> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;

	public GetRuleQueryHandler(IValidator<GetRuleQuery> validator, IUser user, IRepositoryContext db)
	{
		_validator = validator;
		_user = user;
		_db = db;
	}
	
	public async Task<Result<RuleExpandedResponse>> Handle(GetRuleQuery query, CancellationToken ct)
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
		
		var rule = await _db.Rules.GetExpandedResponseIfExistsAsync(query.Rule, endpointId.Value, ct);
		
		return rule != null ? rule : new NotFoundException();
	}
}