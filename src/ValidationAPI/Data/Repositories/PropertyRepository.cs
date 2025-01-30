using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Models;
using ValidationAPI.Domain.Enums;
using Rule = ValidationAPI.Domain.Entities.Rule;

namespace ValidationAPI.Data.Repositories;

public class PropertyRepository : RepositoryBase, IPropertyRepository
{
	public PropertyRepository(IDbConnection connection, IDbTransaction? transaction)
		: base(connection, transaction) { }
	
	public async Task<int> CreateAsync(Property property, CancellationToken ct)
	{
		const string sql = """
			INSERT INTO properties (name, type, is_optional, created_at, modified_at, endpoint_id)
			VALUES (@Name, @Type::propertytype, @IsOptional, @CreatedAt, @ModifiedAt, @EndpointId)
			RETURNING id;
		""";
		
		// Since Dapper does not support custom type handlers for Enums (https://github.com/DapperLib/Dapper/issues/259),
		// we need to manually convert 'Type' enum into string.
		var dParams = new DynamicParameters(property);
		dParams.Add("@Type", property.Type.ToString());
		
		return await Connection.ExecuteScalarAsync<int>(NewCommandDefinition(sql, dParams, ct));
	}

	public async Task<Property?> GetIfExistsAsync(string name, int endpointId, CancellationToken ct)
	{
		const string query = "SELECT * FROM properties WHERE (name, endpoint_id) = (@Name, @EndpointId)";
		
		var command = NewCommandDefinition(query, new { name, endpointId }, ct);
		return await Connection.QueryFirstOrDefaultAsync<Property>(command);
	}
	
	public async Task<PropertyExpandedResponse?> GetExpandedResponseIfExistsAsync(
		string name, int endpointId, bool includeRules, CancellationToken ct)
	{
		string query;
		CommandDefinition command;
		var parameters = new { name, endpointId };
		
		if (!includeRules)
		{
			query = """
				SELECT p.name, p.type, p.is_optional, p.created_at, p.modified_at,
				       e.name AS endpoint
				FROM properties p
				INNER JOIN endpoints e ON e.id = p.endpoint_id
				WHERE (p.name, p.endpoint_id) = (@Name, @EndpointId);
				""";
			
			command = NewCommandDefinition(query, parameters, ct);
			var property = await Connection.QuerySingleOrDefaultAsync<PropertyExpandedResponse>(command);
			if (property is null) return null;
			
			property.Rules = [];
			return property;
		}
		
		query = """
			SELECT e.name,
			       p.name, p.type, p.is_optional, p.created_at, p.modified_at,
			       r.name, r.type, r.value, r.raw_value, r.value_type, r.error_message
			FROM properties p
			INNER JOIN endpoints e ON e.id = p.endpoint_id
			LEFT JOIN rules r ON r.property_id = p.id
			WHERE (p.name, p.endpoint_id) = (@Name, @EndpointId);
			""";
		
		command = NewCommandDefinition(query, parameters, ct);
		
		var propertyEntries = (List<PropertyExpandedResponse>)
			await Connection.QueryAsync<string, Property, Rule?, PropertyExpandedResponse>(command, (e, p, r) =>
				new PropertyExpandedResponse(p.Name, p.Type, p.IsOptional, p.CreatedAt, p.ModifiedAt, e)
				{
					Rules = r != null ? [ r.ToResponse() ] : []
				},
				splitOn: "name");
		
		if (propertyEntries.Count == 0) return null;
		
		var result = propertyEntries.First();
		if (propertyEntries.Count > 1)
		{
			result.Rules = propertyEntries.Select(p => p.Rules.Single()).ToArray();
		}
		return result;
	}
	
	public async Task<List<Property>> GetAllByEndpointIdAsync(int endpointId, CancellationToken ct)
	{
		const string query = "SELECT * FROM properties WHERE endpoint_id = @EndpointId ORDER BY id;";
		
		var command = NewCommandDefinition(query, new { endpointId }, ct);
		
		return (List<Property>)await Connection.QueryAsync<Property>(command);
	}
	
	public Task<List<PropertyMinimalResponse>> GetAllMinimalResponsesAsync(
		Guid userId, int? take, int? offset, PropertyOrder? orderBy, bool desc, CancellationToken ct)
	{
		return GetAllMinimalResponses("e.user_id = @UserId", new { userId }, take, offset, orderBy, desc, ct);
	}
	
	public Task<List<PropertyMinimalResponse>> GetAllMinimalResponsesByEndpointAsync(
		int endpointId, int? take, int? offset, PropertyOrder? orderBy, bool desc, CancellationToken ct)
	{
		return GetAllMinimalResponses("e.id = @EndpointId", new { endpointId }, take, offset, orderBy, desc, ct);
	}
	
	public async Task<int> CountAsync(Guid userId, CancellationToken ct)
	{
		const string query = """
			SELECT count(*)
			FROM properties p
			INNER JOIN endpoints e ON e.id = p.endpoint_id
			WHERE e.user_id = @UserId;
			""";
		
		return await Connection.ExecuteScalarAsync<int>(NewCommandDefinition(query, new { userId }, ct));
	}
	
	public async Task<int> CountAsync(int endpointId, CancellationToken ct)
	{
		const string query = "SELECT count(*) FROM properties WHERE endpoint_id = @EndpointId;";
		return await Connection.ExecuteScalarAsync<int>(NewCommandDefinition(query, new { endpointId }, ct));
	}
	
	public async Task<bool> ExistsAsync(string name, int endpointId, CancellationToken ct)
	{
		const string query = "SELECT EXISTS(SELECT 1 FROM properties WHERE (name, endpoint_id) = (@Name, @EndpointId))";
		
		var command = NewCommandDefinition(query, new { name, endpointId }, ct);
		
		return await Connection.ExecuteScalarAsync<bool>(command);
	}
	
	public async Task<int?> GetIdIfExistsAsync(string name, int endpointId, CancellationToken ct)
	{
		const string query = "SELECT id FROM properties WHERE (name, endpoint_id) = (@Name, @EndpointId);";
		
		var command = NewCommandDefinition(query, new { name, endpointId }, ct);
		return await Connection.ExecuteScalarAsync<int?>(command);
	}
	
	public async Task DeleteAsync(int id, CancellationToken ct)
	{
		const string query = "DELETE FROM properties WHERE id = @Id;";
		await Connection.ExecuteAsync(NewCommandDefinition(query, new { id }, ct));
	}
	
	public Task<PropertyMinimalResponse> SetNameAsync(string newName, int id, CancellationToken ct)
	{
		return SetColumn("name = @NewName", new { newName, id }, ct);
	}
	
	public Task<PropertyMinimalResponse> SetOptionalityAsync(bool isOptional, int id, CancellationToken ct)
	{
		return SetColumn("is_optional = @IsOptional", new { isOptional, id }, ct);
	}
	
	
	private async Task<PropertyMinimalResponse> SetColumn(string column, object parameters, CancellationToken ct)
	{
		string query = $"""
			WITH updated_property AS (
				UPDATE properties
				SET {column}, modified_at = now() AT TIME ZONE 'utc'
				WHERE id = @Id
				RETURNING *
			)
			SELECT p.name, p.type, p.is_optional, p.created_at, p.modified_at,
			       e.name AS endpoint
			FROM updated_property p
			INNER JOIN endpoints e ON e.id = p.endpoint_id;
			""";
		
		var command = NewCommandDefinition(query, parameters, ct);
		return await Connection.QuerySingleAsync<PropertyMinimalResponse>(command);
	}
	
	private async Task<List<PropertyMinimalResponse>> GetAllMinimalResponses(
		string condition, object parameters, int? take, int? offset, PropertyOrder? orderBy, bool desc, CancellationToken ct)
	{
		string query = $"""
			SELECT p.name, p.type, p.is_optional, p.created_at, p.modified_at,
			       e.name AS endpoint
			FROM properties p
			INNER JOIN endpoints e ON e.id = p.endpoint_id
			WHERE {condition}
			ORDER BY {orderBy?.ToDbName() ?? "p.id"} {(desc ? "DESC" : "ASC")}
			LIMIT {(take.HasValue ? take.Value.ToString() : "ALL")} OFFSET {offset ?? 0};
			""";
		
		var command = NewCommandDefinition(query, parameters, ct);
		return (List<PropertyMinimalResponse>) await Connection.QueryAsync<PropertyMinimalResponse>(command);
	}
	
	private CommandDefinition NewCommandDefinition(string query, object parameters, CancellationToken ct)
		=> new(query, parameters, Transaction, cancellationToken: ct);
}