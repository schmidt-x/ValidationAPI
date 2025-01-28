using System;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Models;

public record PropertyResponse(
	string Name,
	PropertyType Type,
	bool IsOptional,
	DateTimeOffset CreatedAt,
	DateTimeOffset ModifiedAt,
	RuleResponse[] Rules);

public static class PropertyExtensions
{
	public static PropertyResponse ToResponse(this Property property, RuleResponse[] rules)
		=> new(property.Name, property.Type, property.IsOptional, property.CreatedAt, property.ModifiedAt, rules);
}