using System;
using ValidationAPI.Domain.Common;

namespace ValidationAPI.Domain.Entities;

public class User : BaseAuditableEntity
{
	public Guid Id { get; init; }
	public string Email { get; init; } = null!;
	public string NormalizedEmail { get; init; } = null!;
	public string Username { get; init; } = null!;
	public string NormalizedUsername { get; init; } = null!;
	public string PasswordHash { get; init; } = null!;
	public bool IsConfirmed { get; init; }
}