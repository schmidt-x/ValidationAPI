using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Serilog;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Rules.Commands.UpdateErrorMessage;

public record UpdateErrorMessageCommand(string Rule, string Endpoint, string? ErrorMessage);

public class UpdateErrorMessageCommandHandler : RequestHandlerBase
{
	private readonly IValidator<UpdateErrorMessageCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;

	public UpdateErrorMessageCommandHandler(
		IValidator<UpdateErrorMessageCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Result<RuleExpandedResponse>> Handle(UpdateErrorMessageCommand command, CancellationToken ct)
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
		
		var ruleId = await _db.Rules.GetIdIfExistsAsync(command.Rule, endpointId.Value, ct);
		if (!ruleId.HasValue)
		{
			return new NotFoundException();
		}
		
		RuleExpandedResponse updatedRule;
		
		try
		{
			updatedRule = await _db.Rules.SetErrorMessageAsync(command.ErrorMessage, ruleId.Value, ct);
		}
		catch (Exception ex)
		{
			_logger.Error("[{UserId}] [{Action}] {ErrorMessage}", userId, "RuleUpdateErrorMessage", ex.Message);
			throw;
		}
		
		_logger.Information(
			"[{UserId}] [{Action}] [{RuleId}] Error message updated.", userId, "RuleUpdateErrorMessage", ruleId.Value);
		
		return updatedRule;
	}
}