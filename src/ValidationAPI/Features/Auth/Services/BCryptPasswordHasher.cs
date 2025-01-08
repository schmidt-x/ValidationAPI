using BCryptHasher = BCrypt.Net.BCrypt;
using System;

namespace ValidationAPI.Features.Auth.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
	public string Hash(string password)
	{
		if (string.IsNullOrWhiteSpace(password))
			throw new ArgumentNullException(nameof(password));
		
		return BCryptHasher.HashPassword(password);
	}

	public bool Verify(string password, string passwordHash)
	{
		if (string.IsNullOrWhiteSpace(password))
			throw new ArgumentNullException(nameof(password));
		
		if (string.IsNullOrWhiteSpace(passwordHash))
			throw new ArgumentNullException(nameof(passwordHash));
		
		return BCryptHasher.Verify(password, passwordHash);
	}
}