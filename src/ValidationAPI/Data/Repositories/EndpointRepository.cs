using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Models;
using Rule = ValidationAPI.Domain.Entities.Rule;

namespace ValidationAPI.Data.Repositories;

public class EndpointRepository : RepositoryBase, IEndpointRepository
{
	public EndpointRepository(IDbConnection connection, IDbTransaction? transaction)
		: base(connection, transaction) { }
	
	
	public async Task<bool> ExistsAsync(string name, Guid userId, CancellationToken ct)
	{
		const string sql = """
			SELECT EXISTS(
				SELECT 1 FROM endpoints
				WHERE (normalized_name, user_id) = (@NormalizedName, @UserId));
			""";
		
		var command = new CommandDefinition(
			sql, new { NormalizedName = name.ToUpperInvariant(), userId }, Transaction, cancellationToken: ct);
		
		return await Connection.ExecuteScalarAsync<bool>(command);
	}	

	public async Task<int> CreateAsync(Endpoint endpoint, CancellationToken ct)
	{
		const string sql = """
			INSERT INTO endpoints (name, normalized_name, user_id)
			VALUES (@Name, @NormalizedName, @UserId)
			RETURNING id;
		""";
		
		var command = new CommandDefinition(sql, endpoint, Transaction, cancellationToken: ct);
		
		return await Connection.ExecuteScalarAsync<int>(command);
	}

	public async Task<int?> GetIdIfExistsAsync(string endpoint, Guid userId, CancellationToken ct)
	{
		const string query = "SELECT id FROM endpoints WHERE (normalized_name, user_id) = (@NormalizedName, @UserId);";
		
		var command = new CommandDefinition(
			query, new { NormalizedName = endpoint.ToUpperInvariant(), userId }, Transaction, cancellationToken: ct);
		
		return await Connection.QuerySingleOrDefaultAsync<int?>(command);
	}
	
	public async Task<EndpointResponse?> GetModelIfExistsAsync(
		string endpointName, Guid userId, bool includePropertiesAndRules, CancellationToken ct)
	{
		string query;
		CommandDefinition command;
		
		if (!includePropertiesAndRules)
		{
			// TODO: include Created and Updated dates
			query = "SELECT name FROM endpoints WHERE (normalized_name, user_id) = (@NormalizedName, @UserId);";
			
			command = new CommandDefinition(
				query, new { NormalizedName = endpointName.ToUpperInvariant(), userId }, Transaction, cancellationToken: ct);
			
			return (await Connection.QuerySingleOrDefaultAsync<Endpoint>(command))?.ToResponse([]);
		}
		
		// TODO: include Created and Updated dates for Endpoint
		query = """
			SELECT e.id, e.name,
			       p.id, p.name, p.type, p.is_optional,
			       r.id, r.name, r.type, r.value, r.raw_value, r.error_message, r.property_id
			FROM endpoints e
			LEFT JOIN properties p ON p.endpoint_id = e.id
			LEFT JOIN rules r ON r.property_id = p.id
			WHERE (e.normalized_name, e.user_id) = (@NormalizedName, @UserId);
			""";
		
		command = new CommandDefinition(
				query, new { NormalizedName = endpointName.ToUpperInvariant(), userId }, Transaction, cancellationToken: ct);
		
		var queryResult = (List<Endpoint>)
			await Connection.QueryAsync<Endpoint, Property, Rule?, Endpoint>(command, (e, p, r) =>
			{
				e.Properties = [ p ];
				p.Rules = r != null ? [ r ] : [];
				return e;
			});
		
		if (queryResult.Count == 0) return null;
		
		// select all rules and group them by their PropertyId
		var groupedRules = queryResult
			.SelectMany(e => e.Properties)
			.Where(p => p.Rules.Count > 0)
			.Select(p => p.Rules.Single())
			.GroupBy(r => r.PropertyId)
			.ToDictionary(g => g.Key);
		
		// assign grouped rules to each property,
		// additionally filtering property entries and converting Entities into response Models
		var responseProperties = queryResult
			.SelectMany(e => e.Properties)
			.DistinctBy(p => p.Id)
			.Select(p =>
			{
				var responseRules = groupedRules.TryGetValue(p.Id, out var rules)
					? rules.Select(r => r.ToResponse()).ToArray()
					: [];
				return p.ToResponse(responseRules);
			})
			.ToArray();
		
		return queryResult.First().ToResponse(responseProperties);
	}
}
