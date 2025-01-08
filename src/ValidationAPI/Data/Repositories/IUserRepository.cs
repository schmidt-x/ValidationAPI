using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Domain.Entities;

namespace ValidationAPI.Data.Repositories;

public interface IUserRepository
{
	Task CreateUserAsync(User user, CancellationToken ct);
	Task<bool> EmailExistsAsync(string emailAddress, CancellationToken ct);
	Task<bool> UsernameExistsAsync(string username, CancellationToken ct);
}