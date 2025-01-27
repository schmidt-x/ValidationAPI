﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using FluentValidation;
using ValidationAPI.Common.Delegates;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Common.Validators.RuleValidators;
using ValidationAPI.Features.Infra;
using ValidationException = ValidationAPI.Common.Exceptions.ValidationException;

namespace ValidationAPI.Features.Properties.Commands.CreateProperty;

public record CreatePropertyCommand(string Endpoint, PropertyRequestExpanded Property);

public class CreatePropertyCommandHandler : RequestHandlerBase
{
	private readonly IValidator<CreatePropertyCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;
	
	public CreatePropertyCommandHandler(
		IValidator<CreatePropertyCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Exception?> Handle(CreatePropertyCommand command, CancellationToken ct)
	{
		var validationResult = _validator.Validate(command);
		if (!validationResult.IsValid)
		{
			return new ValidationException(validationResult.Errors);
		}
		
		command.Deconstruct(out var endpointName, out var propertyRequest);
		
		var userId = _user.Id();
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(endpointName, userId, ct);
		if (!endpointId.HasValue)
		{
			return new OperationInvalidException($"Endpoint '{endpointName}' does not exist.");
		}
		
		if (await _db.Properties.NameExistsAsync(propertyRequest.Name, endpointId.Value, ct))
		{
			return new OperationInvalidException($"Property with name '{propertyRequest.Name}' already exists.");
		}
		
		var dbRuleNames = (await _db.Rules.GetAllNamesAsync(endpointId.Value, ct)).ToHashSet();
		
		var duplicateRule = propertyRequest.Rules.FirstOrDefault(r => dbRuleNames.Contains(r.Name.ToUpperInvariant()));
		if (duplicateRule != null)
		{
			return new OperationInvalidException($"Rule with name '{duplicateRule.Name}' already exists.");
		}
		
		RuleValidator validator = propertyRequest.Type switch
		{
			PropertyType.String   => RuleValidators.ValidateString,
			PropertyType.Int      => throw new NotImplementedException(),
			PropertyType.Float    => throw new NotImplementedException(),
			PropertyType.DateTime => throw new NotImplementedException(),
			PropertyType.DateOnly => throw new NotImplementedException(),
			PropertyType.TimeOnly => throw new NotImplementedException(),
			_ => throw new ArgumentOutOfRangeException(nameof(command))
		};
		
		var dbProperties = (await _db.Properties.GetAllAsync(endpointId.Value, ct))
			.ToDictionary(p => p.Name, p => new PropertyRequest(p.Type, p.IsOptional));
		
		Dictionary<string, List<ErrorDetail>> failures = [];
		
		var validatedRules = validator.Invoke(
			"Property.Rules", propertyRequest.Name, propertyRequest.Rules, dbProperties, failures);
		
		if (validatedRules is null)
		{
			Debug.Assert(failures.Count > 0);
			return new ValidationException(failures);
		}
		
		Debug.Assert(failures.Count == 0);
		
		var timeNow = DateTimeOffset.UtcNow;
		
		var property = new Property
		{
			Name = propertyRequest.Name,
			Type = propertyRequest.Type,
			IsOptional = propertyRequest.IsOptional,
			CreatedAt = timeNow,
			ModifiedAt = timeNow,
			EndpointId = endpointId.Value,
			Rules = validatedRules
		};
		
		await _db.BeginTransactionAsync(ct);
		
		try
		{
			var propertyId = await _db.Properties.CreateAsync(property, ct);
			if (property.Rules.Count != 0)
			{
				await _db.Rules.CreateAsync(property.Rules, propertyId, endpointId.Value, ct);
			}
			await _db.Endpoints.SetModificationDateAsync(timeNow, endpointId.Value, ct);
		}
		catch (Exception ex)
		{
			await _db.UndoChangesAsync();
			_logger.Error(
				"[{UserId}] [{Action}] [{EndpointId}] Failed at creating property: {ErrorMessage}", // TODO:
				userId, "CreateProperty", endpointId.Value, ex.Message);
			throw;
		}
		
		await _db.SaveChangesAsync(ct);
		
		_logger.Information(
			"[{UserId}] [{Action}] [{EndpointId}] New property {PropertyName} created.", // TODO:
			userId, "CreateProperty", endpointId.Value, property.Name);
		
		return null;
	}
}