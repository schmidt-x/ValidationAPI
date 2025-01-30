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

namespace ValidationAPI.Features.Properties.Commands.UpdateOptionality;

public record UpdateOptionalityCommand(string Property, string Endpoint, bool IsOptional);

public class UpdateOptionalityCommandHandler : RequestHandlerBase
{
	private readonly IValidator<UpdateOptionalityCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;

	public UpdateOptionalityCommandHandler(
		IValidator<UpdateOptionalityCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Result<PropertyMinimalResponse>> Handle(UpdateOptionalityCommand command, CancellationToken ct)
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
		
		var property = await _db.Properties.GetIfExistsAsync(command.Property, endpointId.Value, ct);
		if (property is null)
		{
			return new NotFoundException();
		}
		
		if (property.IsOptional == command.IsOptional)
		{
			return new OperationInvalidException($"Property is already {(command.IsOptional ? "optional" : "required")}.");
		}
		
		if (command.IsOptional) // if making optional, check if any rule is referencing
		{
			var ruleName = await  _db.Rules.GetReferencingRuleNameIfExistsAsync(command.Property, property.EndpointId, ct);
			if (ruleName != null)
			{
				return new OperationInvalidException(
					$"Unable to make optional. The property is referenced by at least one rule, specifically '{ruleName}'.");
			}
		}
		
		PropertyMinimalResponse updatedProperty;
		
		try
		{
			updatedProperty = await _db.Properties.SetOptionalityAsync(command.IsOptional, property.Id, ct);
		}
		catch (Exception ex)
		{
			_logger.Error("[{UserId}] [{Action}] {ErrorMessage}", userId, "PropertyUpdateOptionality", ex.Message);
			throw;
		}
		
		_logger.Information(
			"[{UserId}] [{Action}] [{PropertyId}] " +
				"Property {PropertyName} is now " + (updatedProperty.IsOptional ? "optional." : "required."),
			userId, "PropertyUpdateOptionality", property.Id, property.Name);
		
		return updatedProperty;
	}
}