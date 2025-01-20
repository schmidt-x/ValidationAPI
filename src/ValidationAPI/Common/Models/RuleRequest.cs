using System.Text.Json;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Common.Models;

public record RuleRequest(string Name, RuleType Type, JsonElement Value, string ErrorMessage);