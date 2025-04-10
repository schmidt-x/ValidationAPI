﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Domain.Models;

namespace ValidationAPI.Data.Repositories;

public interface IEndpointRepository
{
	Task<bool> ExistsAsync(string name, Guid userId, CancellationToken ct);
	Task<int> CreateAsync(Endpoint endpoint, CancellationToken ct);
	Task<int?> GetIdIfExistsAsync(string name, Guid userId, CancellationToken ct);
	
	Task<EndpointExpandedResponse?> GetExpandedResponseIfExistsAsync(
		string name, Guid userId, bool includePropertiesAndRules, CancellationToken ct);
	
	Task<int> CountAsync(Guid userId, CancellationToken ct);
	
	Task<List<EndpointResponse>> GetAllResponsesAsync(
		Guid userId, int? take, int? offset, EndpointOrder? orderBy, bool desc, CancellationToken ct);
	
	Task<EndpointResponse> RenameAsync(string newName, string newNormalizedName, int endpointId, CancellationToken ct);
	Task<EndpointResponse> UpdateDescriptionAsync(string? description, int endpointId, CancellationToken ct);
	Task DeleteAsync(int endpointId, CancellationToken ct);
	Task SetModificationDateAsync(DateTimeOffset modifiedAt, int endpointId, CancellationToken ct);
}