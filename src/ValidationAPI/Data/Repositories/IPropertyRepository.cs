using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Domain.Entities;

namespace ValidationAPI.Data.Repositories;

public interface IPropertyRepository
{
	Task<int> CreateAsync(Property property, CancellationToken ct);
}