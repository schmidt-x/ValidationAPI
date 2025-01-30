using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Serilog;
using ValidationAPI.Common.Delegates;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Common.Validators.RuleValidators;
using ValidationAPI.Data;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Infra;
using ValidationException = ValidationAPI.Common.Exceptions.ValidationException;

namespace ValidationAPI.Features.Rules.Commands.CreateRules;

public record CreateRulesCommand(string Endpoint, string Property, RuleRequest[] Rules);

public class CreateRulesCommandHandler : RequestHandlerBase
{
	private readonly IValidator<CreateRulesCommand> _validator;
	private readonly IUser _user;
	private readonly IRepositoryContext _db;
	private readonly ILogger _logger;
	
	public CreateRulesCommandHandler(
		IValidator<CreateRulesCommand> validator, IUser user, IRepositoryContext db, ILogger logger)
	{
		_validator = validator;
		_user = user;
		_db = db;
		_logger = logger;
	}
	
	public async Task<Exception?> Handle(CreateRulesCommand command, CancellationToken ct)
	{
		var validationResult = _validator.Validate(command);
		if (!validationResult.IsValid)
		{
			Console.WriteLine("from here");
			return new ValidationException(validationResult.Errors);
		}
		
		var userId = _user.Id();
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(command.Endpoint, userId, ct);
		if (!endpointId.HasValue)
		{
			return new OperationInvalidException($"Endpoint '{command.Endpoint}' does not exist.");
		}
		
		var dbProperty = await _db.Properties.GetIfExistsAsync(command.Property, endpointId.Value, ct);
		if (dbProperty is null)
		{
			return new OperationInvalidException($"Property '{command.Property}' does not exist.");
		}
		
		var dbRuleNames = 
			(await _db.Rules.GetAllNamesAsync(endpointId.Value, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
		
		var duplicateRule = command.Rules.FirstOrDefault(r => dbRuleNames.Contains(r.Name));
		if (duplicateRule != null)
		{
			return new OperationInvalidException($"Rule with the name '{duplicateRule.Name}' already exists (case-insensitive).");
		}
		
		RuleValidator ruleValidator = dbProperty.Type switch
		{
			PropertyType.String   => RuleValidators.ValidateString,
			PropertyType.Int      => throw new NotImplementedException(),
			PropertyType.Float    => throw new NotImplementedException(),
			PropertyType.DateTime => throw new NotImplementedException(),
			PropertyType.DateOnly => throw new NotImplementedException(),
			PropertyType.TimeOnly => throw new NotImplementedException(),
			_ => throw new ArgumentOutOfRangeException(nameof(command))
		};
		
		var dbProperties = (await _db.Properties.GetAllByEndpointIdAsync(endpointId.Value, ct))
			.ToDictionary(p => p.Name, p => new PropertyRequest(p.Type, p.IsOptional));
		
		Dictionary<string, List<ErrorDetail>> failures = [];
		
		var validatedRules = ruleValidator.Invoke(
			nameof(command.Rules), dbProperty.Name, command.Rules, dbProperties, failures);
		
		if (validatedRules is null)
		{
			Debug.Assert(failures.Count > 0);
			return new ValidationException(failures);
		}
		
		Debug.Assert(failures.Count == 0);
		
		await _db.BeginTransactionAsync(ct);
		
		try
		{
			await _db.Rules.CreateAsync(validatedRules, dbProperty.Id, dbProperty.EndpointId, ct);
			await _db.Endpoints.SetModificationDateAsync(DateTimeOffset.UtcNow, endpointId.Value, ct);
		}
		catch (Exception ex)
		{
			await _db.UndoChangesAsync();
			_logger.Error("[{UserId}] [{Action}] {ErrorMessage}", userId, "CreateRules", ex.Message);
			throw;
		}
		
		await _db.SaveChangesAsync(ct);
		
		_logger.Information( // TODO: what should I log here?
			"[{UserId}] [{Action}] Created bunch of rules for property.", userId, "CreateRules");
		
		return null;
	}
}