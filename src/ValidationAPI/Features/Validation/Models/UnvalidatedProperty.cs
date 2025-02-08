using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Features.Validation.Models;

public record UnvalidatedProperty(int Id, string Name, PropertyType Type, object Value);