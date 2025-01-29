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

namespace ValidationAPI.Features.Endpoints.Commands.UpdateDescription;

public record UpdateDescriptionCommand(string Endpoint, string? Description);

public class UpdateDescriptionCommandHandler : RequestHandlerBase
{
	private readonly IValidator<UpdateDescriptionCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;
	
	public UpdateDescriptionCommandHandler(
		IValidator<UpdateDescriptionCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Result<EndpointResponse>> Handle(UpdateDescriptionCommand command, CancellationToken ct)
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
		
		EndpointResponse endpoint;
		try
		{
			endpoint = await _db.Endpoints.UpdateDescriptionAsync(command.Description, endpointId.Value, ct);
		}
		catch (Exception ex)
		{
			_logger.Error("[{UserId}] [{Action}] {ErrorMessage}", userId, "EndpointUpdateDescription", ex.Message);
			throw;
		}
		
		// TODO: should I include the updated description?
		_logger.Information(
			"[{UserId}] [{Action}] [{EndpointId}] Description updated.", userId, "EndpointUpdateDescription", endpointId.Value);
		
		return endpoint;
	}
}