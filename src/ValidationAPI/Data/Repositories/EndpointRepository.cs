using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ValidationAPI.Domain.Entities;

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
}