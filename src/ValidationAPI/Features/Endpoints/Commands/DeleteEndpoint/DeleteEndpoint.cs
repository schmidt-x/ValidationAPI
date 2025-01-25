using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features.Endpoints.Commands.DeleteEndpoint;

public record DeleteEndpointCommand(string Endpoint);

public class DeleteEndpointCommandHandler : RequestHandlerBase
{
	private readonly IValidator<DeleteEndpointCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;
	
	public DeleteEndpointCommandHandler(
		IValidator<DeleteEndpointCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Exception?> Handle(DeleteEndpointCommand command, CancellationToken ct)
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
		
		try
		{
			await _db.Endpoints.DeleteAsync(endpointId.Value, ct);
		}
		catch (Exception ex)
		{
			_logger.Error(
				"[{UserId}] [{Action}] [{EndpointId}] Failed to delete an endpoint: {ErrorMessage}",
				userId, "DeleteEndpoint", endpointId.Value, ex.Message);
			throw;
		}
		
		_logger.Information(
			"[{UserId}] [{Action}] [{EndpointId}] Endpoint is deleted.", userId, "DeleteEndpoint", endpointId.Value);
		
		return null;
	}
}