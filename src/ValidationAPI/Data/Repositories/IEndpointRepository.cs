using System;
using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Domain.Entities;

namespace ValidationAPI.Data.Repositories;

public interface IEndpointRepository
{
	Task<bool> ExistsAsync(string name, Guid userId, CancellationToken ct);
	Task<int> CreateAsync(Endpoint endpoint, CancellationToken ct);
	Task<int?> GetIdIfExistsAsync(string endpoint, Guid userId, CancellationToken ct);
}