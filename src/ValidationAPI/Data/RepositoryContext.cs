using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using ValidationAPI.Data.Repositories;

namespace ValidationAPI.Data;

public sealed class RepositoryContext : IRepositoryContext, IDisposable
{
	private readonly NpgsqlConnection _connection;
	private NpgsqlTransaction? _transaction;
	
	public RepositoryContext(NpgsqlDataSource source)
	{
		_connection = source.CreateConnection();
	}
	
	private IUserRepository? _users;
	public IUserRepository Users => _users ??= new UserRepository(_connection, _transaction);
	
	private IEndpointRepository? _endpoints;
	public IEndpointRepository Endpoints => _endpoints ??= new EndpointRepository(_connection, _transaction);
	
	private IPropertyRepository? _properties;
	public IPropertyRepository Properties => _properties ??= new PropertyRepository(_connection, _transaction);
	
	private IRuleRepository? _rules;
	public IRuleRepository Rules => _rules ??= new RuleRepository(_connection, _transaction);
	
	
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
		_users = null; _endpoints = null; _properties = null; _rules = null;
	}
}