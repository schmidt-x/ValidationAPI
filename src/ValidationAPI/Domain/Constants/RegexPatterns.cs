namespace ValidationAPI.Domain.Constants;

public static class RegexPatterns
{
	public const string Endpoint = @"^[a-zA-Z0-9\-.]+$";
	public const string Property = @"^[a-zA-Z_]\w*$";
}