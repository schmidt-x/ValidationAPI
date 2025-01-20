﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rule = ValidationAPI.Domain.Entities.Rule;
using Dapper;

namespace ValidationAPI.Data.Repositories;

public class RuleRepository : RepositoryBase, IRuleRepository
{
	public RuleRepository(IDbConnection connection, IDbTransaction? transaction)
		: base(connection, transaction) { }

	public async Task CreateAsync(List<Rule> rules, int propertyId, int endpointId, CancellationToken ct)
	{
		string sql = $"""
			INSERT INTO rules (name, normalized_name, type, value, raw_value, extra_info, is_relative, error_message,
			                   property_id, endpoint_id)
			VALUES (@Name, @NormalizedName, @Type::ruletype, @Value, @RawValue, @ExtraInfo, @IsRelative, @ErrorMessage,
			        {propertyId}, {endpointId})
			RETURNING id;
			""";
		
		// Since Dapper does not support custom type handlers for Enums (https://github.com/DapperLib/Dapper/issues/259),
		// we need to manually convert 'Type' enum into string.
		var anonymousRules = rules.Select(r => new
		{
			r.Name,
			r.NormalizedName,
			Type = r.Type.ToString(),
			r.Value,
			r.RawValue,
			r.ExtraInfo,
			r.IsRelative,
			r.ErrorMessage
		});
		
		var command = new CommandDefinition(sql, anonymousRules, Transaction, cancellationToken: ct);
		await Connection.ExecuteAsync(command);
	}
}