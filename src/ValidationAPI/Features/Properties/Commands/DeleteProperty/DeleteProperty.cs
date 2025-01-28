using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Serilog;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Properties.Commands.DeleteProperty;

public record DeletePropertyCommand(string Property, string Endpoint);

public class DeletePropertyCommandHandler : RequestHandlerBase
{
	private readonly IValidator<DeletePropertyCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;
	
	public DeletePropertyCommandHandler(
		IValidator<DeletePropertyCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Exception?> Handle(DeletePropertyCommand command, CancellationToken ct)
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
		
		var propertyId = await _db.Properties.GetIdIfExistsAsync(command.Property, endpointId.Value, ct);
		if (!propertyId.HasValue)
		{
			return new NotFoundException();
		}
		
		var ruleName = await _db.Rules.GetReferencingRuleNameIfExistsAsync(command.Property, endpointId.Value, ct);
		if (ruleName != null)
		{
			return new OperationInvalidException(
				$"Unable to delete. The property is referenced by at least one rule, specifically '{ruleName}'.");
		}
		
		try
		{
			await _db.Properties.DeleteAsync(propertyId.Value, ct);
		}
		catch (Exception ex)
		{
			_logger.Error(
				"[{UserId}] [{Action}] [{EndpointId}] Failed to delete a property: {ErrorMessage}",
				userId, "DeleteProperty", endpointId.Value, ex.Message);
			
			throw;
		}
		
		_logger.Information(
			"[{UserId}] [{Action}] [{EndpointId}] Property {PropertyName} deleted.",
			userId, "DeleteProperty", endpointId.Value, command.Property);
		
		return null;
	}
}