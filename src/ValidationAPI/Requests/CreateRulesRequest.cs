using ValidationAPI.Common.Models;

namespace ValidationAPI.Requests;

public record CreateRulesRequest(string Endpoint, string Property, RuleRequest[] Rules);