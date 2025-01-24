using System;
using System.Collections.Generic;
using ValidationAPI.Domain.Common;

namespace ValidationAPI.Domain.Entities;

public class Endpoint : BaseAuditableEntity
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public string NormalizedName { get; init; } = null!;
	public string? Description { get; init; }
	public Guid UserId { get; init; }
	
	public List<Property> Properties { get; set; } = null!;
}