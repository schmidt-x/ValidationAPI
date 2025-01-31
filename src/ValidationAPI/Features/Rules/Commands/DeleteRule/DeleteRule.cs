using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Rules.Commands.DeleteRule;

public record DeleteRuleCommand(string Rule, string Endpoint);

public class DeleteRuleCommandHandler : RequestHandlerBase
{
	private readonly IValidator<DeleteRuleCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;

	public DeleteRuleCommandHandler(
		IValidator<DeleteRuleCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Exception?> Handle(DeleteRuleCommand command, CancellationToken ct)
	{
		if (!_validator.Validate(command).IsValid)
		{
			return new NotFoundException();
		}
		
		var userId = _user.Id();
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(command.Endpoint, userId, ct);
		if (!endpointId.HasValue)
		{
			return new NotFoundException();
		}
		
		Rule? rule = await _db.Rules.GetIfExistsAsync(command.Rule, endpointId.Value, ct);
		if (rule is null)
		{
			return new NotFoundException();
		}
		
		await _db.BeginTransactionAsync(ct);
		
		try
		{
			await _db.Rules.DeleteAsync(rule.Id, ct);
			var timeNow = DateTimeOffset.UtcNow;
			
			await _db.Properties.SetModificationDateAsync(timeNow, rule.PropertyId, ct);
			await _db.Endpoints.SetModificationDateAsync(timeNow, rule.EndpointId, ct);
		}
		catch (Exception ex)
		{
			await _db.UndoChangesAsync();
			_logger.Error("[{UserId}] [{Action}] {ErrorMessage}", userId, "DeleteRule", ex.Message);
			throw;
		}
		
		await _db.SaveChangesAsync(ct);
		
		_logger.Information(
			"[{UserId}] [{Action}] [{RuleId}] Rule {RuleName} deleted.", userId, "DeleteRule", rule.Id, rule.Name);
		
		return null;
	}
}