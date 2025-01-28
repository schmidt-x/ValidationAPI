using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rule = ValidationAPI.Domain.Entities.Rule;

namespace ValidationAPI.Data.Repositories;

public interface IRuleRepository
{
	Task CreateAsync(List<Rule> rules, int propertyId, int endpointId, CancellationToken ct);
	Task<List<Rule>> GetAllByPropertyIdAsync(IEnumerable<int> propertyIds, CancellationToken ct);
	Task<List<string>> GetAllNamesAsync(int endpointId, CancellationToken ct);
	Task<string?> GetReferencingRuleNameIfExistsAsync(string propertyName, int propertyId, CancellationToken ct);
}