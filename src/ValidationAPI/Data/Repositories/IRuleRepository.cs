using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rule = ValidationAPI.Domain.Entities.Rule;

namespace ValidationAPI.Data.Repositories;

public interface IRuleRepository
{
	Task CreateAsync(List<Rule> rules, int propertyId, int endpointId, CancellationToken ct);
	Task<ICollection<Rule>> GetAllByPropertyIdAsync(IEnumerable<int> propertyIds, CancellationToken ct);
}