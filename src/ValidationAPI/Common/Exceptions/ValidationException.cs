using System;
using System.Collections.Generic;
using FluentValidation.Results;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Common.Exceptions;

public class ValidationException : Exception
{
	public string Code => ErrorCodes.VALIDATION_FAILURE;
	
	public Dictionary<string, List<ErrorDetail>> Errors { get; } = new();
	
	private ValidationException() : base("One or more validation errors occurred.")
	{ }
	
	public ValidationException(string property, string code, string message) : this()
	{
		Errors.Add(property, [ new ErrorDetail(code, message) ]);
	}
	
	public ValidationException(List<ValidationFailure> failures) : this()
	{
		foreach (var failure in failures)
		{
			if (Errors.TryGetValue(failure.PropertyName, out var errorDetails))
			{
				errorDetails.Add(new ErrorDetail(failure.ErrorCode, failure.ErrorMessage));
			}
			else
			{
				Errors.Add(failure.PropertyName, [ new ErrorDetail(failure.ErrorCode, failure.ErrorMessage) ]);
			}
		}
	}
}