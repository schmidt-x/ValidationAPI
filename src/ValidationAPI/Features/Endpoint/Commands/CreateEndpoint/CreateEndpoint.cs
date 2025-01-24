using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Serilog;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Infra;
using ValidationAPI.Features.Endpoint.Commands.CreateEndpoint.Validators;
using ValidationException = ValidationAPI.Common.Exceptions.ValidationException;

namespace ValidationAPI.Features.Endpoint.Commands.CreateEndpoint;

public record CreateEndpointCommand(
	string Endpoint, string? Description, Dictionary<string, PropertyRequest> Properties);

public class CreateEndpointCommandHandler : RequestHandlerBase
{
	private readonly IValidator<CreateEndpointCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;
	
	public CreateEndpointCommandHandler(
		IValidator<CreateEndpointCommand> validator,
		IUser user,
		IRepositoryContext db,
		ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}

	
	public async Task<Exception?> Handle(CreateEndpointCommand command, CancellationToken ct)
	{
		var validationResult = _validator.Validate(command);
		if (!validationResult.IsValid)
		{
			return new ValidationException(validationResult.Errors);
		}
		
		Dictionary<string, List<ErrorDetail>> failures = [];
		List<Property> validatedProperties = [];
		
		foreach (var property in command.Properties)
		{
			PropertyRuleValidator validator = property.Value.Type switch
			{
				PropertyType.String   => PropertyRuleValidators.ValidateString,
				PropertyType.Int      => throw new NotImplementedException(),
				PropertyType.Float    => throw new NotImplementedException(), // TODO: combine with Int?
				PropertyType.DateTime => throw new NotImplementedException(), // TODO: combine with the following two?
				PropertyType.DateOnly => throw new NotImplementedException(),
				PropertyType.TimeOnly => throw new NotImplementedException(),
				_ => throw new ArgumentOutOfRangeException(nameof(command))
			};
			
			var validatedProperty = validator.Invoke(property, command.Properties, failures);
			if (validatedProperty != null)
			{
				validatedProperties.Add(validatedProperty);
			}
		}
		
		if (failures.Count != 0)
		{
			return new ValidationException(failures);
		}
		
		var userId = _user.Id();
		if (await _db.Endpoints.ExistsAsync(command.Endpoint, userId, ct))
		{
			return new OperationInvalidException($"Endpoint '{command.Endpoint}' already exists.");
		}
		
		var timeNow = DateTimeOffset.UtcNow;
		
		var endpoint = new Domain.Entities.Endpoint
		{
			Name = command.Endpoint,
			NormalizedName = command.Endpoint.ToUpperInvariant(),
			Description = command.Description,
			CreatedAt = timeNow,
			ModifiedAt = timeNow,
			UserId = userId
		};
		
		await _db.BeginTransactionAsync(ct);
		
		try
		{
			var endpointId = await _db.Endpoints.CreateAsync(endpoint, ct);
			
			foreach (var property in validatedProperties)
			{
				property.EndpointId = endpointId;
				var propertyId = await _db.Properties.CreateAsync(property, ct);
				if (property.Rules.Count == 0) continue;
				await _db.Rules.CreateAsync(property.Rules, propertyId, endpointId, ct);
			}
			
			await _db.SaveChangesAsync(ct);
		}
		catch (Exception ex)
		{
			await _db.UndoChangesAsync();
			_logger.Error(
				"[{UserId}] [{Action}] Failed to create an endpoint: {ErrorMessage}", userId, "CreateEndpoint", ex.Message);
			throw;
		}
		
		_logger.Information(
			"[{UserId}] [{Action}] New endpoint is created: {Endpoint}.", userId, "CreateEndpoint", endpoint.Name);
		
		return null;
	}

	
	private delegate Property? PropertyRuleValidator(
		KeyValuePair<string, PropertyRequest> property,
		Dictionary<string, PropertyRequest> properties,
		Dictionary<string, List<ErrorDetail>> failures);
}