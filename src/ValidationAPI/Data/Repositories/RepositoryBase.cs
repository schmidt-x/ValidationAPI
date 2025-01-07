using System.Data;

namespace ValidationAPI.Data.Repositories;

public abstract class RepositoryBase
{
	private readonly IDbConnection _connection;
	
	protected IDbConnection Connection
		=> Transaction is not null ? Transaction.Connection! : _connection;
		
	protected IDbTransaction? Transaction { get; }

	protected RepositoryBase(IDbConnection connection, IDbTransaction? transaction)
		=> (_connection, Transaction) = (connection, transaction);
}