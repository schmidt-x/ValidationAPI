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
		const string sql = 
			"SELECT EXISTS(SELECT 1 FROM endpoints WHERE (normalized_name, user_id) = (@NormalizedName, @UserId));";
		
		var command = new CommandDefinition(
			sql, new { NormalizedName = name.ToUpperInvariant(), userId }, Transaction, cancellationToken: ct);
		
		return await Connection.ExecuteScalarAsync<bool>(command);
	}

	public async Task<int> CreateAsync(Endpoint endpoint, CancellationToken ct)
	{
		const string sql = """
			INSERT INTO endpoints (name, normalized_name, description, created_at, modified_at, user_id)
			VALUES (@Name, @NormalizedName, @Description, @CreatedAt, @ModifiedAt, @UserId)
			RETURNING id;
		""";
		
		var command = new CommandDefinition(sql, endpoint, Transaction, cancellationToken: ct);
		
		return await Connection.ExecuteScalarAsync<int>(command);
	}

	public async Task<int?> GetIdIfExistsAsync(string name, Guid userId, CancellationToken ct)
	{
		const string query = "SELECT id FROM endpoints WHERE (normalized_name, user_id) = (@NormalizedName, @UserId);";
		
		var command = new CommandDefinition(
			query, new { NormalizedName = name.ToUpperInvariant(), userId }, Transaction, cancellationToken: ct);
		
		return await Connection.QuerySingleOrDefaultAsync<int?>(command);
	}
	
	public async Task<EndpointExpandedResponse?> GetExpandedResponseIfExistsAsync(
		string name, Guid userId, bool includePropertiesAndRules, CancellationToken ct)
	{
		string query;
		CommandDefinition command;
		
		if (!includePropertiesAndRules)
		{
			query = """
				SELECT name, description, created_at, modified_at
				FROM endpoints
				WHERE (normalized_name, user_id) = (@NormalizedName, @UserId);
				""";
			command = new CommandDefinition(
				query, new { NormalizedName = name.ToUpperInvariant(), userId }, Transaction, cancellationToken: ct);
			
			return (await Connection.QuerySingleOrDefaultAsync<Endpoint>(command))?.ToResponse([]);
		}
		
		query = """
			SELECT e.id, e.name, e.description, e.created_at, e.modified_at,
			       p.id, p.name, p.type, p.is_optional,
			       r.id, r.name, r.type, r.value, r.value_type, r.raw_value, r.error_message, r.property_id
			FROM endpoints e
			LEFT JOIN properties p ON p.endpoint_id = e.id
			LEFT JOIN rules r ON r.property_id = p.id
			WHERE (e.normalized_name, e.user_id) = (@NormalizedName, @UserId);
			""";
		
		command = new CommandDefinition(
				query, new { NormalizedName = name.ToUpperInvariant(), userId }, Transaction, cancellationToken: ct);
		
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
	
	public async Task<IReadOnlyCollection<EndpointResponse>> GetAllResponsesAsync(Guid userId, CancellationToken ct)
	{
		const string query = """
			SELECT name, description, created_at, modified_at
			FROM endpoints
			WHERE user_id = @UserId;
			""";
		
		var command = new CommandDefinition(query, new { userId }, Transaction, cancellationToken: ct);
		
		var endpoints = await Connection.QueryAsync<EndpointResponse>(command);
		
		return (IReadOnlyCollection<EndpointResponse>)endpoints;
	}
	
	public async Task<EndpointResponse> RenameAsync(RenameEndpoint endpoint, int endpointId, CancellationToken ct)
	{
		const string query = """
			UPDATE endpoints
			SET name = @NewName,
			    normalized_name = @NewNormalizedName,
			    modified_at = now() AT TIME ZONE 'utc'
			WHERE id = @EndpointId
			RETURNING name, description, created_at, modified_at;
			""";
		
		var dParams = new DynamicParameters(endpoint);
		dParams.Add(nameof(endpointId), endpointId);
		
		var command = new CommandDefinition(query, dParams, Transaction, cancellationToken: ct);
		
		return await Connection.QuerySingleAsync<EndpointResponse>(command);
	}
	
	public async Task DeleteAsync(int endpointId, CancellationToken ct)
	{
		const string query = "DELETE FROM endpoints WHERE id = @EndpointId;";
		
		await Connection.ExecuteAsync(
			new CommandDefinition(query, new { endpointId }, Transaction, cancellationToken: ct));
	}
}
