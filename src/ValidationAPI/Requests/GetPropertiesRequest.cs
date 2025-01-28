using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Requests;

public class GetPropertiesRequest
{
	[FromQuery] public string? Endpoint { get; init; }
	[FromQuery] public int? PageNumber { get; init; } = 1;
	[FromQuery] public int? PageSize { get; init; } = 5;
	[FromQuery] public PropertyOrder? OrderBy { get; init; }
	[FromQuery] public bool? Desc { get; init; } = false;
}