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

	public async Task<PropertyExpandedResponse?> GetExpandedResponseIfExistsAsync(
		string name, int endpointId, bool includeRules, CancellationToken ct)
	{
		string query;
		CommandDefinition command;
		var parameters = new { name, endpointId };
		
		if (!includeRules)
		{
			query = """
				SELECT e.name AS endpoint, p.name, p.type, p.is_optional, p.created_at, p.modified_at
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
				new PropertyExpandedResponse(e, p.Name, p.Type, p.IsOptional, p.CreatedAt, p.ModifiedAt)
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
		const string query = "select * from properties where endpoint_id = @EndpointId;";
		
		var command = NewCommandDefinition(query, new { endpointId }, ct);
		
		return (List<Property>)await Connection.QueryAsync<Property>(command);
	}
	
	public async Task<bool> NameExistsAsync(string name, int endpointId, CancellationToken ct)
	{
		const string query = "SELECT EXISTS(SELECT 1 FROM properties WHERE (name, endpoint_id) = (@Name, @EndpointId))";
		
		var command = NewCommandDefinition(query, new { name, endpointId }, ct);
		
		return await Connection.ExecuteScalarAsync<bool>(command);
	}
	
	public async Task<List<Property>> GetAllAsync(int endpointId, CancellationToken ct)
	{
		const string query = "select * from properties where endpoint_id = @EndpointId;";
		
		var command = NewCommandDefinition(query, new { endpointId }, ct);
		
		return (List<Property>)await Connection.QueryAsync<Property>(command);
	}
	
	
	private CommandDefinition NewCommandDefinition(string query, object parameters, CancellationToken ct)
		=> new(query, parameters, Transaction, cancellationToken: ct);
}