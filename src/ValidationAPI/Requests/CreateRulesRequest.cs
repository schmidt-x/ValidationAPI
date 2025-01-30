using ValidationAPI.Common.Models;

namespace ValidationAPI.Requests;

public class CreateRulesRequest
{
	public string Endpoint { get; init; } = null!;
	public string Property { get; init; } = null!;
	public RuleRequest[] Rules { get; init; } = null!;
}