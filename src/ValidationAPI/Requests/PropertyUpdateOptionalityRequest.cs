using Microsoft.AspNetCore.Mvc;

namespace ValidationAPI.Requests;

public class PropertyUpdateOptionalityRequest
{
	[FromRoute] public string Property { get; init; } = null!;
	[FromQuery] public string Endpoint { get; init; } = null!;
	[FromForm] public bool IsOptional { get; init; }
}
