using System;
using ValidationAPI.Domain.Constants;

namespace ValidationAPI.Common.Exceptions;

public class AuthException : Exception
{
	public override string? Source { get; set; } = ErrorCodes.AUTH_FAILURE;
	
	public AuthException() : base("Login/password is incorrect.")
	{ }
}