using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Requests;

public class GetRulesRequest
{
	[FromQuery] public string? Endpoint { get; init; }
	[FromQuery] public int? PageNumber { get; init; }
	[FromQuery] public int? PageSize { get; init; }
	[FromQuery] public RuleOrder? OrderBy { get; init; }
	[FromQuery] public bool? Desc { get; init; }
}