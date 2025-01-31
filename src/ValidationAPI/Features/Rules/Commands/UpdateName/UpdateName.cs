using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Serilog;
using ValidationException = ValidationAPI.Common.Exceptions.ValidationException;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Rules.Commands.UpdateName;

public record UpdateNameCommand(string Rule, string Endpoint, string NewName);

public class UpdateNameCommandHandler : RequestHandlerBase
{
	private readonly IValidator<UpdateNameCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;

	public UpdateNameCommandHandler(
		IValidator<UpdateNameCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Result<RuleExpandedResponse>> Handle(UpdateNameCommand command, CancellationToken ct)
	{
		var validationResult = _validator.Validate(command);
		if (!validationResult.IsValid)
		{
			return validationResult.Errors.Any(f => f.PropertyName is nameof(command.Rule) or nameof(command.Endpoint))
				? new NotFoundException()
				: new ValidationException(validationResult.Errors);
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
		
		if (await _db.Rules.NameExistsAsync(command.NewName, endpointId.Value, ct))
		{
			return new OperationInvalidException($"Rule with the name '{command.NewName}' already exists (case-insensitive).");
		}
		
		RuleExpandedResponse updatedRule;
		
		try
		{
			updatedRule = await _db.Rules.SetNameAsync(command.NewName, ruleId.Value, ct);
		}
		catch (Exception ex)
		{
			_logger.Error("[{UserId}] [{Action}] {ErrorMessage}", userId, "RuleUpdateName", ex.Message);
			throw;
		}
		
		_logger.Information(
			"[{UserId}] [{Action}] [{PropertyId}] " + $"'{command.Rule}' -> '{command.NewName}'",
			userId, "RuleUpdateName", ruleId.Value);
		
		return updatedRule;
	}
}