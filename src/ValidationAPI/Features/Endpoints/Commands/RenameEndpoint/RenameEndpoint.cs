using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Models;
using ValidationAPI.Features.Infra;
using ValidationException = ValidationAPI.Common.Exceptions.ValidationException;

namespace ValidationAPI.Features.Endpoints.Commands.RenameEndpoint;

public record RenameEndpointCommand(string Endpoint, string NewName);

public class RenameEndpointCommandHandler : RequestHandlerBase
{
	private readonly IValidator<RenameEndpointCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;
	
	public RenameEndpointCommandHandler(
		IValidator<RenameEndpointCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Result<EndpointResponse>> Handle(RenameEndpointCommand command, CancellationToken ct)
	{
		var validationResult = _validator.Validate(command);
		if (!validationResult.IsValid)
		{
			return validationResult.Errors.Any(f => f.PropertyName == nameof(command.Endpoint))
				? new NotFoundException()
				: new ValidationException(validationResult.Errors);
		}
		
		var userId = _user.Id();
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(command.Endpoint, userId, ct);
		if (!endpointId.HasValue)
		{
			return new NotFoundException();
		}
		
		if (await _db.Endpoints.ExistsAsync(command.NewName, userId, ct))
		{
			return new OperationInvalidException(
				$"Endpoint with the name '{command.NewName}' already exists (case-insensitive).");
		}
		
		EndpointResponse response;
		
		try
		{
			response = await _db.Endpoints.RenameAsync
				(command.NewName, command.NewName.ToUpperInvariant(), endpointId.Value, ct);
		}
		catch (Exception ex)
		{
			_logger.Error("[{UserId}] [{Action}] {ErrorMessage}", userId, "RenameEndpoint", ex.Message);
			throw;
		}
		
		_logger.Information(
			"[{UserId}] [{Action}] [{EndpointId}] " + $"'{command.Endpoint}' -> '{command.NewName}'",
			userId, "RenameEndpoint", endpointId.Value);
		
		return response;
	}
}