using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Models;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Data.Repositories;

public interface IPropertyRepository
{
	Task<int> CreateAsync(Property property, CancellationToken ct);
	Task<Property?> GetIfExistsAsync(string name, int endpointId, CancellationToken ct);
	
	Task<PropertyExpandedResponse?> GetExpandedResponseIfExistsAsync(
		string name, int endpointId, bool includeRules, CancellationToken ct);
	
	Task<List<PropertyMinimalResponse>> GetAllMinimalResponsesAsync(
		Guid userId, int? take, int? offset, PropertyOrder? orderBy, bool desc, CancellationToken ct);
	
	Task<List<PropertyMinimalResponse>> GetAllMinimalResponsesByEndpointAsync(
		int endpointId, int? take, int? offset, PropertyOrder? orderBy, bool desc, CancellationToken ct);
	
	Task<List<Property>> GetAllByEndpointIdAsync(int endpointId, CancellationToken ct);
	Task<int> CountAsync(Guid userId, CancellationToken ct);
	Task<int> CountAsync(int endpointId, CancellationToken ct);
	Task<bool> ExistsAsync(string name, int endpointId, CancellationToken ct);
	Task<int?> GetIdIfExistsAsync(string name, int endpointId, CancellationToken ct);
	Task DeleteAsync(int id, CancellationToken ct);
	Task<PropertyMinimalResponse> SetNameAsync(string newName, int id, CancellationToken ct);
	Task<PropertyMinimalResponse> SetOptionalityAsync(bool isOptional, int id, CancellationToken ct);
}