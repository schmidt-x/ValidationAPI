using System;

namespace ValidationAPI.Domain.Entities;

public class User
{
	public Guid Id { get; init; }
	public string Email { get; init; } = null!;
	public string NormalizedEmail { get; init; } = null!;
	public string Username { get; init; } = null!;
	public string NormalizedUsername { get; init; } = null!;
	public string PasswordHash { get; init; } = null!;
	public bool IsConfirmed { get; init; }
	public DateTimeOffset CreatedAt { get; init; }
	public DateTimeOffset ModifiedAt { get; init; }
}