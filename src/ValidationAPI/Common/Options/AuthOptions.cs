namespace ValidationAPI.Common.Options;

public class AuthOptions
{
	public const string Section = "Auth";
	
	public int UsernameMinLength { get; init; }
	public int UsernameMaxLength { get; init; }
	public int PasswordMinLength { get; init; }
}