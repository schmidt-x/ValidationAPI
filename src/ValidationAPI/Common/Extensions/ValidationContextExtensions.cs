using FluentValidation;
using FluentValidation.Results;

namespace ValidationAPI.Common.Extensions;

public static class ValidationContextExtensions
{
	public static void AddFailure<T>(
		this ValidationContext<T> context, string propertyName, string errorCode, string errorMessage)
	{
		context.AddFailure(new ValidationFailure(propertyName, errorMessage) { ErrorCode = errorCode} );
	}
}