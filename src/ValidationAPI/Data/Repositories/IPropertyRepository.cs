using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Domain.Entities;

namespace ValidationAPI.Data.Repositories;

public interface IPropertyRepository
{
	Task<int> CreateAsync(Property property, CancellationToken ct);
	Task<List<Property>> GetAllByEndpointIdAsync(int endpointId, CancellationToken ct);
	Task<bool> NameExistsAsync(string name, int endpointId, CancellationToken ct);
	Task<List<Property>> GetAllAsync(int endpointId, CancellationToken ct);
}