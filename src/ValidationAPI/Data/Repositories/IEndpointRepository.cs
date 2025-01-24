using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Models;

namespace ValidationAPI.Data.Repositories;

public interface IEndpointRepository
{
	Task<bool> ExistsAsync(string name, Guid userId, CancellationToken ct);
	Task<int> CreateAsync(Endpoint endpoint, CancellationToken ct);
	Task<int?> GetIdIfExistsAsync(string name, Guid userId, CancellationToken ct);
	
	Task<EndpointExpandedResponse?> GetExpandedResponseIfExistsAsync(
		string name, Guid userId, bool includePropertiesAndRules, CancellationToken ct);
	
	Task<IReadOnlyCollection<EndpointResponse>> GetAllResponsesAsync(Guid userId, CancellationToken ct);
	Task<EndpointResponse> RenameAsync(RenameEndpoint endpoint, int endpointId, CancellationToken ct);
	Task<EndpointResponse> UpdateDescriptionAsync(string? description, int endpointId, CancellationToken ct);
	Task DeleteAsync(int endpointId, CancellationToken ct);
}