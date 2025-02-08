using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationAPI.Common.Models;
using ValidationAPI.Common.Services;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Data;
using ValidationAPI.Features.Infra;
using ValidationAPI.Features.Validation.Validators;
using ValidationException = ValidationAPI.Common.Exceptions.ValidationException;

namespace ValidationAPI.Features.Validation.Queries.ValidateRequest;

public record ValidateRequestQuery(string Endpoint, Dictionary<string, JsonElement> Body);

public class ValidateRequestQueryHandler : RequestHandlerBase
{
	private readonly IValidator<ValidateRequestQuery> _validator;
	private readonly IRepositoryContext _db;
	private readonly IUser _user;
	
	public ValidateRequestQueryHandler(IValidator<ValidateRequestQuery> validator, IUser user, IRepositoryContext db)
	{
		_validator = validator;
		_user = user;
		_db = db;
	}
	
	public async Task<Result<ValidationResult>> Handle(ValidateRequestQuery query, CancellationToken ct)
	{
		var now = DateTimeOffset.UtcNow;
		
		if (!_validator.Validate(query).IsValid)
		{
			return new NotFoundException();
		}
		
		var endpointId = await _db.Endpoints.GetIdIfExistsAsync(query.Endpoint, _user.Id(), ct);
		if (!endpointId.HasValue)
		{
			return new NotFoundException();
		}
		
		var dbProperties = await _db.Properties.GetAllByEndpointIdAsync(endpointId.Value, ct);
		
		Dictionary<string, List<ErrorDetail>> failures = [];
		
		var unvalidatedProperties = PropertyValidators.ValidateTypes(dbProperties, query.Body, failures);
		if (unvalidatedProperties is null)
		{
			Debug.Assert(failures.Count > 0);
			return new ValidationException(failures);
		}
		
		Debug.Assert(failures.Count == 0);
		
		var dbRules = await _db.Rules.GetAllByPropertyIdAsync(unvalidatedProperties.Select(x => x.Value.Id), ct);
		var sortedRules = dbRules.GroupBy(r => r.PropertyId).ToDictionary(x => x.Key, x => x.ToArray());
		
		PropertyValidators.Validate(unvalidatedProperties, sortedRules, failures, now);
		
		int propertiesProcessed = unvalidatedProperties.Count;
		int rulesApplied = dbRules.Count;
		
		return failures.Count == 0
			? ValidationResult.Success(propertiesProcessed, rulesApplied)
			: ValidationResult.Failure(propertiesProcessed, rulesApplied, failures);
	}
}