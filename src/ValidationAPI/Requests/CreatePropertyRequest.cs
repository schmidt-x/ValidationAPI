using ValidationAPI.Common.Models;

namespace ValidationAPI.Requests;

public record CreatePropertyRequest(string Endpoint, PropertyRequestExpanded Property);
