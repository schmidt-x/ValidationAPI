using System.Collections.Generic;
using ValidationAPI.Common.Exceptions;
using ValidationAPI.Common.Models;

namespace ValidationAPI.Responses;

public record FailResponse(string Code, string Message, Dictionary<string, List<ErrorDetail>> Errors)
{
	public static FailResponse From(AuthException ex) => new(ex.Code, ex.Message, []);
	public static FailResponse From(ValidationException ex) => new(ex.Code, ex.Message, ex.Errors);
}