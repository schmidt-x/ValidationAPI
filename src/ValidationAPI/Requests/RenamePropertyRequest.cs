﻿using Microsoft.AspNetCore.Mvc;

namespace ValidationAPI.Requests;

public class RenamePropertyRequest
{
	[FromRoute] public string Property { get; init; } = null!;
	[FromQuery] public string Endpoint { get; init; } = null!;
	[FromForm] public string NewName { get; init; } = null!;
}