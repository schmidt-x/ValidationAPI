using System;
using System.Collections.Generic;
using FluentValidation.Results;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Common.Exceptions;

public class ValidationException : Exception
{
	public override string? Source { get; set; } = ErrorCodes.VALIDATION_FAILURE;

	public Dictionary<string, List<ErrorDetail>> Errors { get; } = new();
	
	private ValidationException() : base("One or more validation errors occurred.")
	{ }

	public ValidationException(Dictionary<string, List<ErrorDetail>> errors) : this()
		=> Errors = errors;
	
	public ValidationException(string property, string code, string message) : this()
	{
		Errors.Add(property, [ new ErrorDetail(code, message) ]);
	}
	
	public ValidationException(List<ValidationFailure> failures) : this()
	{
		foreach (var failure in failures)
		{
			Errors.AddErrorDetail(failure.PropertyName, failure.ErrorCode, failure.ErrorMessage);
		}
	}
}