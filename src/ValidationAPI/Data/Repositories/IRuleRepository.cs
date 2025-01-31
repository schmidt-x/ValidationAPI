using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Domain.Models;
using Rule = ValidationAPI.Domain.Entities.Rule;

namespace ValidationAPI.Data.Repositories;

public interface IRuleRepository
{
	Task CreateAsync(List<Rule> rules, int propertyId, int endpointId, CancellationToken ct);
	Task<Rule?> GetIfExistsAsync(string name, int endpointId, CancellationToken ct);
	Task DeleteAsync(int id, CancellationToken ct);
	Task<int?> GetIdIfExistsAsync(string name, int endpointId, CancellationToken ct);
	Task<bool> NameExistsAsync(string name, int endpointId, CancellationToken ct);
	Task<RuleExpandedResponse> SetNameAsync(string newName, int id, CancellationToken ct);
	Task<RuleExpandedResponse> SetErrorMessageAsync(string? errorMessage, int id, CancellationToken ct);
	Task<RuleExpandedResponse?> GetExpandedResponseIfExistsAsync(string rule, int endpointId, CancellationToken ct);
	Task<List<Rule>> GetAllByPropertyIdAsync(IEnumerable<int> propertyIds, CancellationToken ct);
	Task<List<string>> GetAllNamesAsync(int endpointId, CancellationToken ct);
	Task<string?> GetReferencingRuleNameIfExistsAsync(string propertyName, int endpointId, CancellationToken ct);
	Task UpdateReferencingRulesAsync(string oldValue, string newValue, int endpointId, CancellationToken ct);
	Task<int> CountAsync(Guid userId, CancellationToken ct);
	Task<int> CountAsync(int endpointId, CancellationToken ct);
	
	Task<List<RuleExpandedResponse>> GetAllExpandedResponsesAsync(
		Guid userId, int? take, int? offset, RuleOrder? orderBy, bool desc, CancellationToken ct);
	
	Task<List<RuleExpandedResponse>> GetAllExpandedResponsesByEndpointAsync(
		int endpointId, int? take, int? offset, RuleOrder? orderBy, bool desc, CancellationToken ct);
}