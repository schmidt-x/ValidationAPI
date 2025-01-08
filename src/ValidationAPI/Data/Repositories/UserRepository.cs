using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ValidationAPI.Domain.Entities;

namespace ValidationAPI.Data.Repositories;

public class UserRepository : RepositoryBase, IUserRepository
{
	public UserRepository(IDbConnection connection, IDbTransaction? transaction)
		: base(connection, transaction) { }

	
	public async Task CreateUserAsync(User user, CancellationToken ct)
	{
		const string sql = """
			INSERT INTO users
				(id, email, normalized_email, username, normalized_username,
				 password_hash, is_confirmed, created_at, modified_at)
			VALUES 
				(@Id, @Email, @NormalizedEmail, @Username, @NormalizedUsername,
				 @PasswordHash, @IsConfirmed, @CreatedAt, @ModifiedAt)
			""";
		
		var command = new CommandDefinition(
			sql, new DynamicParameters(user), Transaction, cancellationToken: ct);
		
		await Connection.ExecuteAsync(command);
	}

	public async Task<bool> EmailExistsAsync(string emailAddress, CancellationToken ct)
	{
		const string sql = "SELECT EXISTS(SELECT 1 FROM users WHERE normalized_email = @EmailAddress)";
		
		var command = new CommandDefinition(
			sql, new { emailAddress = emailAddress.ToUpperInvariant() }, Transaction, cancellationToken: ct);
		
		return await Connection.ExecuteScalarAsync<bool>(command);
	}

	public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct)
	{
		const string sql = "SELECT EXISTS(SELECT 1 FROM users WHERE normalized_username = @Username)";
	
		var command = new CommandDefinition(
			sql, new { username = username.ToUpperInvariant() }, Transaction, cancellationToken: ct);
		
		return await Connection.ExecuteScalarAsync<bool>(command);
	}
	
	public async Task<User?> GetByEmailIfExistsAsync(string emailAddress, CancellationToken ct)
	{
		const string sql = "SELECT * FROM users WHERE normalized_email = @EmailAddress";
		
		var command = new CommandDefinition(
			sql, new { emailAddress = emailAddress.ToUpperInvariant()}, Transaction, cancellationToken: ct);
		
		return await Connection.QuerySingleOrDefaultAsync<User>(command);
	}
}