using System.Text.Json;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Features.Validation.Models;

public record UnvalidatedProperty(int Id, string Name, PropertyType Type, JsonElement Value);