using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace ValidationAPI.Data;

public sealed class RepositoryContext : IRepositoryContext, IDisposable
{
	private readonly NpgsqlConnection _connection;
	
	public RepositoryContext(NpgsqlDataSource source)
	{
		_connection = source.CreateConnection();
	}
	
	private NpgsqlTransaction? _transaction;
	
	
	public async Task BeginTransactionAsync(CancellationToken ct)
	{
		await _connection.OpenAsync(ct);
		_transaction = await _connection.BeginTransactionAsync(ct);
		ResetRepositories();
	}

	public async Task SaveChangesAsync(CancellationToken ct)
	{
		if (_transaction is null)
			throw new InvalidOperationException("No transaction was started");
		
		try
		{
			await _transaction.CommitAsync(ct);
		}
		finally
		{
			await CleanUp();
		}
	}

	public async Task UndoChangesAsync()
	{
		if (_transaction is null)
			throw new InvalidOperationException("No transaction was started");
		
		try
		{
			await _transaction.RollbackAsync();
		}
		finally
		{
			await CleanUp();
		}
	}
	
	public void Dispose()
	{
		_transaction?.Dispose();
		_connection.Dispose();
	}
	
	private async Task CleanUp()
	{
		await _transaction!.DisposeAsync();
		_transaction = null;
		await _connection.CloseAsync();
		ResetRepositories();
	}
	
	private void ResetRepositories()
	{
		
	}
}