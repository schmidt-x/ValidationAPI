namespace ValidationAPI.Domain.Constants;

// ReSharper disable InconsistentNaming

public static class ErrorCodes
{
	// Global
	public const string VALIDATION_FAILURE        = nameof(VALIDATION_FAILURE);
	public const string AUTH_FAILURE              = nameof(AUTH_FAILURE);
	public const string INVALID_OPERATION_FAILURE = nameof(INVALID_OPERATION_FAILURE);
	
	// Auth
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
	
	// Rules
	public const string EMPTY_RULE_VALUE    = nameof(EMPTY_RULE_VALUE);
	public const string INVALID_RULE_VALUE  = nameof(INVALID_RULE_VALUE);
	public const string INVALID_RULE_TYPE   = nameof(INVALID_RULE_TYPE);
	public const string EMPTY_RULE_NAME     = nameof(EMPTY_RULE_NAME);
	public const string DUPLICATE_RULE_NAME = nameof(DUPLICATE_RULE_NAME);
	
	// Properties
	public const string EMPTY_PROPERTY        = nameof(EMPTY_PROPERTY);
	public const string PROPERTY_NOT_PRESENT  = nameof(PROPERTY_NOT_PRESENT);
	public const string INVALID_PROPERTY_TYPE = nameof(INVALID_PROPERTY_TYPE);
	public const string EMPTY_PROPERTY_NAME   = nameof(EMPTY_PROPERTY_NAME);
	public const string INVALID_PROPERTY_NAME = nameof(INVALID_PROPERTY_NAME);
	
	// Endpoints 
	public const string EMPTY_ENDPOINT_NAME     = nameof(EMPTY_ENDPOINT_NAME);
	public const string INVALID_ENDPOINT_NAME   = nameof(INVALID_ENDPOINT_NAME);
	public const string DUPLICATE_ENDPOINT_NAME = nameof(DUPLICATE_ENDPOINT_NAME);
}