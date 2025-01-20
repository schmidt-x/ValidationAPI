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
}