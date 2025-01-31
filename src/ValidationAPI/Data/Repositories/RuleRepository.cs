using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rule = ValidationAPI.Domain.Entities.Rule;
using Dapper;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Domain.Models;
using ValidationAPI.Data.Extensions;

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

	public async Task<RuleExpandedResponse?> GetExpandedResponseIfExistsAsync(
		string rule, int endpointId, CancellationToken ct)
	{
		const string query = """
			SELECT r.name, r.type, r.raw_value, r.value, r.value_type, r.error_message,
			       p.name,
			       e.name
			FROM rules r
			INNER JOIN properties p ON p.id = r.property_id
			INNER JOIN endpoints e ON e.id = p.endpoint_id
			WHERE (r.normalized_name, r.endpoint_id) = (@RuleName, @EndpointId);
			""";
		
		var command = NewCommandDefinition(query, new { ruleName = rule.ToUpperInvariant(), endpointId }, ct);
		
		var ruleEntry = await Connection.QueryAsync<Rule, string, string, RuleExpandedResponse>(
			command, (r, p, e) => r.ToExpandedResponse(p, e), splitOn: "name");
		
		return ruleEntry.SingleOrDefault();
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
		const string query = "SELECT normalized_name FROM rules WHERE endpoint_id = @EndpointId";
		
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
	
	public async Task<int> CountAsync(Guid userId, CancellationToken ct)
	{
		const string query = """
			SELECT count(*)
			FROM endpoints e
			INNER JOIN rules r ON r.endpoint_id = e.id
			WHERE e.user_id = @UserId;
			""";
		
		return await Connection.ExecuteScalarAsync<int>(NewCommandDefinition(query, new { userId }, ct));
	}
	
	public async Task<int> CountAsync(int endpointId, CancellationToken ct)
	{
		const string query = "SELECT count(*) FROM rules WHERE endpoint_id = @EndpointId;";
		
		return await Connection.ExecuteScalarAsync<int>(NewCommandDefinition(query, new { endpointId }, ct));
	}
	
	public Task<List<RuleExpandedResponse>> GetAllExpandedResponsesAsync(
		Guid userId, int? take, int? offset, RuleOrder? orderBy, bool desc, CancellationToken ct)
	{
		return GetAllExpandedResponses("e.user_id = @UserId", new { userId }, take, offset, orderBy, desc, ct);
	}
	
	public Task<List<RuleExpandedResponse>> GetAllExpandedResponsesByEndpointAsync(
		int endpointId, int? take, int? offset, RuleOrder? orderBy, bool desc, CancellationToken ct)
	{
		return GetAllExpandedResponses("e.id = @EndpointId", new { endpointId }, take, offset, orderBy, desc, ct);
	}
	
	
	private async Task<List<RuleExpandedResponse>> GetAllExpandedResponses(
		string condition, object parameters, int? take, int? offset, RuleOrder? orderBy, bool desc, CancellationToken ct)
	{
		string query = $"""
			SELECT e.name,
			       p.name,
			       r.name, r.type, r.value, r.raw_value, r.value_type, r.error_message
			FROM endpoints e
			INNER JOIN properties p ON p.endpoint_id = e.id
			INNER JOIN rules r ON r.property_id = p.id
			WHERE {condition}
			ORDER BY {orderBy?.ToDbName() ?? "r.id"} {(desc ? "DESC" : "ASC")}
			LIMIT {(take.HasValue ? take.Value.ToString() : "ALL")} OFFSET {offset ?? 0}
			""";
		
		var command = NewCommandDefinition(query, parameters, ct);
		
		var rules = await Connection.QueryAsync<string, string, Rule, RuleExpandedResponse>(
			command, (e, p, r) => r.ToExpandedResponse(p, e), splitOn: "name");
		
		return (List<RuleExpandedResponse>)rules;
	}
	
	private CommandDefinition NewCommandDefinition(string query, object parameters, CancellationToken ct)
		=> new(query, parameters, Transaction, cancellationToken: ct);
}