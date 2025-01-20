using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Data.Repositories;

namespace ValidationAPI.Data;

public interface IRepositoryContext
{
	IUserRepository Users { get; }
	IEndpointRepository Endpoints { get; }
	IPropertyRepository Properties { get; }
	IRuleRepository Rules { get; }
	
	Task BeginTransactionAsync(CancellationToken ct);
	Task SaveChangesAsync(CancellationToken ct);
	Task UndoChangesAsync();
}