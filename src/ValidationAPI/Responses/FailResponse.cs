using System;
using ValidationAPI.Common.Exceptions;
using System.Collections.Generic;
using ValidationAPI.Common.Models;

namespace ValidationAPI.Responses;

public record FailResponse(string Code, string Message, Dictionary<string, List<ErrorDetail>> Errors)
{
	public static FailResponse From(Exception ex)
		=> new(ex.Source ?? throw ex, ex.Message, ex is ValidationException vEx ? vEx.Errors : []);
}