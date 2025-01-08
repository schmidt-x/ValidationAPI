using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Data.Repositories;

namespace ValidationAPI.Data;

public interface IRepositoryContext
{
	IUserRepository Users { get; }
	
	Task BeginTransactionAsync(CancellationToken ct);
	Task SaveChangesAsync(CancellationToken ct);
	Task UndoChangesAsync();
}