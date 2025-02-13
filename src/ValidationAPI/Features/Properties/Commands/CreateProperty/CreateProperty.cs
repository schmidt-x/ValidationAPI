using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using FluentValidation;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Data;
using ValidationAPI.Domain.Entities;
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
		
		command.Deconstruct(out var endpointName, out PropertyRequestExpanded propertyReqEx);
		
		var userId = _user.Id();
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(endpointName, userId, ct);
		if (!endpointId.HasValue)
		{
			return new OperationInvalidException($"Endpoint '{endpointName}' does not exist.");
		}
		
		if (await _db.Properties.ExistsAsync(propertyReqEx.Name, endpointId.Value, ct))
		{
			return new OperationInvalidException($"Property with the name '{propertyReqEx.Name}' already exists (case-sensitive).");
		}
		
		if (propertyReqEx.Rules.Length == 0)
		{
			return await SaveProperty(propertyReqEx, [], endpointId.Value, userId, ct);
		}
		
		var dbRuleNames = (await _db.Rules.GetAllNamesAsync(endpointId.Value, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
		
		var duplicateRule = propertyReqEx.Rules.FirstOrDefault(r => dbRuleNames.Contains(r.Name));
		if (duplicateRule != null)
		{
			return new OperationInvalidException($"Rule with the name '{duplicateRule.Name}' already exists (case-insensitive).");
		}
		
		var dbProperties = (await _db.Properties.GetAllByEndpointIdAsync(endpointId.Value, ct))
			.ToDictionary(p => p.Name, p => new PropertyRequest(p.Type, p.IsOptional));
		
		var ruleValidator = new RuleValidator(dbProperties);
		
		var validatedRules = ruleValidator.Validate("Property.Rules", propertyReqEx.Name, propertyReqEx.ToRequest());
		if (validatedRules is null)
		{
			return new ValidationException(ruleValidator.Failures);
		}
		
		return await SaveProperty(propertyReqEx, validatedRules, endpointId.Value, userId, ct);
	}
	
	private async Task<Exception?> SaveProperty(
		PropertyRequestExpanded propertyReqEx, List<Rule> rules, int endpointId, Guid userId, CancellationToken ct)
	{
		var timeNow = DateTimeOffset.UtcNow;
		
		var property = new Property
		{
			Name = propertyReqEx.Name,
			Type = propertyReqEx.Type,
			IsOptional = propertyReqEx.IsOptional,
			CreatedAt = timeNow,
			ModifiedAt = timeNow,
			EndpointId = endpointId,
			Rules = rules
		};
		
		int propertyId;
		await _db.BeginTransactionAsync(ct);
		
		try
		{
			propertyId = await _db.Properties.CreateAsync(property, ct);
			if (property.Rules.Count != 0)
			{
				await _db.Rules.CreateAsync(property.Rules, propertyId, endpointId, ct);
			}
			await _db.Endpoints.SetModificationDateAsync(timeNow, endpointId, ct);
		}
		catch (Exception ex)
		{
			await _db.UndoChangesAsync();
			_logger.Error("[{UserId}] [{Action}] {ErrorMessage}", userId, "CreateProperty", ex.Message);
			throw;
		}
		
		await _db.SaveChangesAsync(ct);
		
		_logger.Information(
			"[{UserId}] [{Action}] [{PropertyId}] New property {PropertyName} created.",
			userId, "CreateProperty", propertyId, property.Name);
		
		return null;
	}
}