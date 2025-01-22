using System;
using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Models;

namespace ValidationAPI.Data.Repositories;

public interface IEndpointRepository
{
	Task<bool> ExistsAsync(string name, Guid userId, CancellationToken ct);
	Task<int> CreateAsync(Endpoint endpoint, CancellationToken ct);
	Task<int?> GetIdIfExistsAsync(string endpoint, Guid userId, CancellationToken ct);
	
	Task<EndpointResponse?> GetModelIfExistsAsync(
		string endpointName, Guid userId, bool includePropertiesAndRules, CancellationToken ct);
}