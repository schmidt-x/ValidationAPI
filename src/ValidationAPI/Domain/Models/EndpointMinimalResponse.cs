using System;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Models;

public record PropertyMinimalResponse(
	string Name, PropertyType Type, bool IsOptional, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, string Endpoint);