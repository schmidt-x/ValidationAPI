namespace ValidationAPI.Domain.Constants;

// ReSharper disable InconsistentNaming

public static class ErrorCodes
{
	public const string VALIDATION_FAILURE = nameof(VALIDATION_FAILURE);
	public const string AUTH_FAILURE       = nameof(AUTH_FAILURE);
	
	public const string EMPTY_VALUE             = nameof(EMPTY_VALUE);
	public const string DUPLICATE_VALUE         = nameof(DUPLICATE_VALUE);
	public const string INVALID_VALUE           = nameof(INVALID_VALUE);
	public const string LENGTH_BELOW_MINIMUM    = nameof(LENGTH_BELOW_MINIMUM);
	public const string LENGTH_ABOVE_MAXIMUM    = nameof(LENGTH_ABOVE_MAXIMUM);
	public const string INVALID_CHAR_IN_VALUE   = nameof(INVALID_CHAR_IN_VALUE);
	public const string VALUE_MISSING_DIGIT     = nameof(VALUE_MISSING_DIGIT);
	public const string VALUE_MISSING_LOWERCASE = nameof(VALUE_MISSING_LOWERCASE);
	public const string VALUE_MISSING_UPPERCASE = nameof(VALUE_MISSING_UPPERCASE);
	public const string VALUE_MISSING_SYMBOL    = nameof(VALUE_MISSING_SYMBOL);
}