using System;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Models;

public record PropertyExpandedResponse(
	string Name, PropertyType Type, bool IsOptional, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, string Endpoint)
{
	public RuleResponse[] Rules { get; set; } = null!;
}