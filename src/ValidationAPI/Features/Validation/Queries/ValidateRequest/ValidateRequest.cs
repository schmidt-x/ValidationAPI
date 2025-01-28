using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using FluentValidation;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Data;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Infra;
using ValidationAPI.Features.Validation.Queries.ValidateRequest.Validators;
using static ValidationAPI.Domain.Constants.ErrorCodes;
using ValidationException = ValidationAPI.Common.Exceptions.ValidationException;

namespace ValidationAPI.Features.Validation.Queries.ValidateRequest;

public record ValidateRequestQuery(string Endpoint, Dictionary<string, JsonElement> Body);

public record UnvalidatedProperty(int Id, string Name, PropertyType Type, JsonElement Value);

public class ValidateRequestQueryHandler : RequestHandlerBase
{
	private readonly IValidator<ValidateRequestQuery> _validator;
	private readonly IRepositoryContext _db;
	private readonly IUser _user;
	
	public ValidateRequestQueryHandler(
		IValidator<ValidateRequestQuery> validator, IUser user, IRepositoryContext db)
	{
		_validator = validator;
		_user = user;
		_db = db;
	}
	
	public async Task<Result<ValidationResult>> Handle(ValidateRequestQuery query, CancellationToken ct)
	{
		if (!_validator.Validate(query).IsValid)
		{
			return new NotFoundException();
		}
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(query.Endpoint, _user.Id(), ct);
		if (!endpointId.HasValue)
		{
			return new NotFoundException();
		}
		
		Dictionary<string, List<ErrorDetail>> failures = [];
		List<UnvalidatedProperty> unvalidatedProperties = [];
		
		var dbProperties = await _db.Properties.GetAllByEndpointIdAsync(endpointId.Value, ct);
		
		foreach (var dbProperty in dbProperties)
		{
			if (!query.Body.TryGetValue(dbProperty.Name, out var requestValue))
			{
				if (!dbProperty.IsOptional)
				{
					failures.AddErrorDetail(dbProperty.Name, PROPERTY_NOT_PRESENT,
						"Property is not present. Consider making it optional.");
				}
				continue;
			}
			
			switch (dbProperty.Type)
			{
				case PropertyType.Int:
				case PropertyType.Float:
					if (requestValue.ValueKind != JsonValueKind.Number)
					{
						failures.AddErrorDetail(dbProperty.Name, INVALID_PROPERTY_TYPE, 
							$"Expected property type is Number; got: {requestValue.ValueKind}.");
						continue;
					}
					break;
				case PropertyType.String:
				case PropertyType.DateTime:
				case PropertyType.DateOnly:
				case PropertyType.TimeOnly:
					if (requestValue.ValueKind != JsonValueKind.String)
					{
						failures.AddErrorDetail(dbProperty.Name, INVALID_PROPERTY_TYPE,
							$"Expected property type is String; got: {requestValue.ValueKind}.");
						continue;
					}
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(query));
			}
			
			if (failures.Count != 0) continue;
			
			unvalidatedProperties.Add(
				new UnvalidatedProperty(dbProperty.Id, dbProperty.Name, dbProperty.Type, requestValue));
		}
		
		if (failures.Count != 0)
		{
			return new ValidationException(failures);
		}
		
		var dbRules = await _db.Rules.GetAllByPropertyIdAsync(unvalidatedProperties.Select(x => x.Id), ct);
		var sortedRules = dbRules.GroupBy(r => r.PropertyId).ToDictionary(x => x.Key, x => x.ToArray());
		
		foreach (var property in unvalidatedProperties)
		{
			if (!sortedRules.TryGetValue(property.Id, out var rules))
				continue; // no rules for a property
			
			PropertyValidator validator = property.Type switch
			{
				PropertyType.String   => PropertyValidators.ValidateString,
				PropertyType.Int      => throw new NotImplementedException(),
				PropertyType.Float    => throw new NotImplementedException(),
				PropertyType.DateTime => throw new NotImplementedException(),
				PropertyType.DateOnly => throw new NotImplementedException(),
				PropertyType.TimeOnly => throw new NotImplementedException(),
				_ => throw new ArgumentOutOfRangeException(nameof(query))
			};
			
			validator.Invoke(property, rules, query.Body, failures);
		}
		
		int propertiesProcessed = unvalidatedProperties.Count;
		int rulesApplied = dbRules.Count;
		
		return failures.Count == 0
			? ValidationResult.Success(propertiesProcessed, rulesApplied)
			: ValidationResult.Failure(propertiesProcessed, rulesApplied, failures);
	}
	
	
	private delegate void PropertyValidator(
		UnvalidatedProperty property,
		Rule[] rules,
		Dictionary<string, JsonElement> requestBody,
		Dictionary<string, List<ErrorDetail>> failures);
}