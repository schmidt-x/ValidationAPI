using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ValidationAPI.Domain.Entities;

namespace ValidationAPI.Data.Repositories;

public class PropertyRepository : RepositoryBase, IPropertyRepository
{
	public PropertyRepository(IDbConnection connection, IDbTransaction? transaction)
		: base(connection, transaction) { }
	
	public async Task<int> CreateAsync(Property property, CancellationToken ct)
	{
		const string sql = """
			INSERT INTO properties (name, type, is_optional, endpoint_id)
			VALUES (@Name, @Type::propertytype, @IsOptional, @EndpointId)
			RETURNING id;
		""";
		
		// Since Dapper does not support custom type handlers for Enums (https://github.com/DapperLib/Dapper/issues/259),
		// we need to manually convert 'Type' enum into string.
		var dParams = new DynamicParameters(property);
		dParams.Add("@Type", property.Type.ToString());
		
		var command = new CommandDefinition(sql, dParams, Transaction, cancellationToken: ct);
		return await Connection.ExecuteScalarAsync<int>(command);
	}

	public async Task<List<Property>> GetAllByEndpointIdAsync(int endpointId, CancellationToken ct)
	{
		const string query = "select * from properties where endpoint_id = @EndpointId;";
		
		var command = new CommandDefinition(query, new { endpointId }, Transaction, cancellationToken: ct);
		
		return (List<Property>)await Connection.QueryAsync<Property>(command);
	}
	
	public async Task<bool> NameExistsAsync(string name, int endpointId, CancellationToken ct)
	{
		const string query = "SELECT EXISTS(SELECT 1 FROM properties WHERE (name, endpoint_id) = (@Name, @EndpointId))";
		
		var command = new CommandDefinition(query, new { name, endpointId }, Transaction, cancellationToken: ct);
		
		return await Connection.ExecuteScalarAsync<bool>(command);
	}
	
	public async Task<List<Property>> GetAllAsync(int endpointId, CancellationToken ct)
	{
		const string query = "select * from properties where endpoint_id = @EndpointId;";
		
		var command = new CommandDefinition(query, new { endpointId }, Transaction, cancellationToken: ct);
		
		return (List<Property>)await Connection.QueryAsync<Property>(command);
	}
}