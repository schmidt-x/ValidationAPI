using ValidationAPI.Domain.Enums;
namespace ValidationAPI.Domain.Models;

public record PropertyExpandedResponse(string Endpoint, string Name, PropertyType Type, bool IsOptional)
{
	public RuleResponse[] Rules { get; set; } = null!;
}
