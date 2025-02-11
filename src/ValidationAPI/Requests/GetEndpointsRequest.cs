using Microsoft.AspNetCore.Mvc;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Requests;

public class GetEndpointsRequest
{
	[FromQuery] public int? PageNumber { get; init; }
	[FromQuery] public int? PageSize { get; init; }
	[FromQuery] public EndpointOrder? OrderBy { get; init; }
	[FromQuery] public bool? Desc { get; init; }
}