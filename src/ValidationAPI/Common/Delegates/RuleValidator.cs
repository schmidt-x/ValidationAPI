using System.Collections.Generic;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;

namespace ValidationAPI.Common.Delegates;

public delegate List<Rule>? RuleValidator(
	string failureKey,
	string propertyName,
	RuleRequest[] rules,
	Dictionary<string, PropertyRequest> properties,
	Dictionary<string, List<ErrorDetail>> failures);