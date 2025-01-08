using ValidationAPI.Common.Exceptions;
using ValidationAPI.Responses;

namespace ValidationAPI.Extensions;

public static class ValidationExceptionExtensions
{
	public static FailResponse ToFailResponse(this ValidationException ex) => new(ex.Code, ex.Message, ex.Errors);
}