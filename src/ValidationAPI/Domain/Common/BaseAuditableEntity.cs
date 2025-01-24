using System;

namespace ValidationAPI.Domain.Common;

public abstract class BaseAuditableEntity
{
	public DateTimeOffset CreatedAt { get; init; }
	public DateTimeOffset ModifiedAt { get; init; }
}