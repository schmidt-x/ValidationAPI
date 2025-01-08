using System.Collections.Generic;
using ValidationAPI.Common.Models;

namespace ValidationAPI.Responses;

public record FailResponse(string Code, string Message, Dictionary<string, List<ErrorDetail>>? Errors);