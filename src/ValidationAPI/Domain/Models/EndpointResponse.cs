using System;

namespace ValidationAPI.Domain.Models;

public class EndpointResponse(string name, string? description, DateTimeOffset createdAt, DateTimeOffset modifiedAt)
{
	public string Name { get; } = name;
	public string Description { get; } = description ?? string.Empty;
	public DateTimeOffset CreatedAt { get; } = createdAt;
	public DateTimeOffset ModifiedAt { get; } = modifiedAt;
}