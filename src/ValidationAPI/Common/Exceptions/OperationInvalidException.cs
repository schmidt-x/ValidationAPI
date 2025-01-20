using System;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Common.Exceptions;

public class OperationInvalidException : Exception
{
	public override string? Source { get; set; } = ErrorCodes.INVALID_OPERATION_FAILURE;

	public OperationInvalidException(string message) : base(message)
	{ }
}