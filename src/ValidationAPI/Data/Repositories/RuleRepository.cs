using System.Collections.Generic;
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
			INSERT INTO rules (name, normalized_name, type, value, value_type, raw_value, extra_info,
			                   is_relative, error_message, property_id, endpoint_id)
			
			VALUES (@Name, @NormalizedName, @Type::ruletype, @Value, @ValueType::rulevaluetype, @RawValue, @ExtraInfo,
			        @IsRelative, @ErrorMessage, {propertyId}, {endpointId})
			
			RETURNING id;
			""";
		
		// Since Dapper does not support custom type handlers for Enums (https://github.com/DapperLib/Dapper/issues/259),
		// we need to manually convert 'Type' and 'ValueType' enums into string.
		var anonymousRules = rules.Select(r => new
		{
			r.Name,
			r.NormalizedName,
			Type = r.Type.ToString(),
			r.Value,
			ValueType = r.ValueType.ToString(),
			r.RawValue,
			r.ExtraInfo,
			r.IsRelative,
			r.ErrorMessage
		});
		
		var command = new CommandDefinition(sql, anonymousRules, Transaction, cancellationToken: ct);
		await Connection.ExecuteAsync(command);
	}

	public async Task<List<Rule>> GetAllByPropertyIdAsync(IEnumerable<int> propertyIds, CancellationToken ct)
	{
		const string query = "SELECT * FROM rules WHERE property_id = ANY (@PropertyIds)";
		
		var command = new CommandDefinition(
			query, new { PropertyIds = propertyIds.ToArray() }, Transaction, cancellationToken: ct);
		
		return (List<Rule>)await Connection.QueryAsync<Rule>(command);
	}
	
	public async Task<List<string>> GetAllNamesAsync(int endpointId, CancellationToken ct)
	{
		const string query = "select normalized_name from rules where endpoint_id = @EndpointId";
		
		var command = new CommandDefinition(query, new { endpointId }, Transaction, cancellationToken: ct);
		
		return (List<string>)await Connection.QueryAsync<string>(command); 
	}
	
	public async Task<string?> GetReferencingRuleNameIfExistsAsync(string propertyName, int endpointId, CancellationToken ct)
	{
		const string query = """
			SELECT name
			FROM rules
			WHERE is_relative = true AND endpoint_id = @EndpointId AND value = @PropertyName
			LIMIT 1;
			""";
		
		var command = new CommandDefinition(query, new { propertyName, endpointId }, Transaction, cancellationToken: ct);
		return await Connection.ExecuteScalarAsync<string?>(command);
	}
	
	public async Task UpdateReferencingRulesAsync(string oldValue, string newValue, int endpointId, CancellationToken ct)
	{
		const string query = """
			UPDATE rules
			SET value = @NewValue, raw_value = overlay(raw_value PLACING @NewValue FROM 2 FOR @Length)
			WHERE endpoint_id = @EndpointId AND is_relative AND value = @OldValue;
			""";
		
		var command = NewCommandDefinition(query, new { oldValue, newValue, oldValue.Length, endpointId }, ct);
		await Connection.ExecuteAsync(command);
	}
	
	
	private CommandDefinition NewCommandDefinition(string query, object parameters, CancellationToken ct)
		=> new(query, parameters, Transaction, cancellationToken: ct);
}