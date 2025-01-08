namespace ValidationAPI.Features.Auth.Services;

public interface IPasswordHasher
{
	string Hash(string password);
	bool Verify(string password, string passwordHash);
}