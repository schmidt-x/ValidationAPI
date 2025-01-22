using System.Collections.Generic;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Entities;

public class Property
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public PropertyType Type { get; init; }
	public bool IsOptional { get; init; }
	public int EndpointId { get; set; }
	
	public List<Rule> Rules { get; set; } = null!;
}