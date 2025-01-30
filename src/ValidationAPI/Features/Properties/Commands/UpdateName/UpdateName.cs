using System;
using System.Linq;
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
using ValidationException = ValidationAPI.Common.Exceptions.ValidationException;

namespace ValidationAPI.Features.Properties.Commands.UpdateName;

public record UpdateNameCommand(string Property, string Endpoint, string NewName);

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
	
	public async Task<Result<PropertyMinimalResponse>> Handle(UpdateNameCommand command, CancellationToken ct)
	{
		var validationResult = _validator.Validate(command);
		if (!validationResult.IsValid)
		{
			return validationResult.Errors.Any(f => f.PropertyName is nameof(command.Property) or nameof(command.Endpoint))
				? new NotFoundException()
				: new ValidationException(validationResult.Errors);
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
		
		if (await _db.Properties.ExistsAsync(command.NewName, endpointId.Value, ct))
		{
			return new OperationInvalidException($"Property with the name '{command.NewName}' already exists (case-sensitive).");
		}
		
		PropertyMinimalResponse updatedProperty;
		await _db.BeginTransactionAsync(ct);
		
		try
		{
			updatedProperty = await _db.Properties.SetNameAsync(command.NewName, propertyId.Value, ct);
			await _db.Rules.UpdateReferencingRulesAsync(command.Property, command.NewName, endpointId.Value, ct);
		}
		catch (Exception ex)
		{
			await _db.UndoChangesAsync();
			_logger.Error("[{UserId}] [{Action}] {ErrorMessage}", userId, "PropertyUpdateName", ex.Message);
			throw;
		}
		
		await _db.SaveChangesAsync(ct);
		
		_logger.Information(
			"[{UserId}] [{Action}] [{PropertyId}] " + $"'{command.Property}' -> '{command.NewName}'",
			userId, "PropertyUpdateName", propertyId.Value);
		
		return updatedProperty;
	}
}