using System.Threading;
using System.Threading.Tasks;

namespace ValidationAPI.Data;

public interface IRepositoryContext
{
	Task BeginTransactionAsync(CancellationToken ct);
	Task SaveChangesAsync(CancellationToken ct);
	Task UndoChangesAsync();
}