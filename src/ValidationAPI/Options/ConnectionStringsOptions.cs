namespace ValidationAPI.Options;

public class ConnectionStringsOptions
{
	public const string Section = "ConnectionStrings";

	public string Postgres { get; init; } = null!;
}