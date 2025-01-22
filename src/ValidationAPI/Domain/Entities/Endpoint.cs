using System;
using System.Collections.Generic;

namespace ValidationAPI.Domain.Entities;

public class Endpoint
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public string NormalizedName { get; init; } = null!;
	public Guid UserId { get; init; }
	
	public List<Property> Properties { get; set; } = null!;
}