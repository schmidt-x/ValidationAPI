using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Entities;

public class Rule
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public string NormalizedName { get; init; } = null!;
	public RuleType Type { get; init; }
	public string Value { get; init; } = null!;
	public string? RawValue { get; init; }
	public string? ExtraInfo { get; init; }
	public bool IsRelative { get; init; }
	public string? ErrorMessage { get; init; }
	public int PropertyId { get; init; }
	public int EndpointId { get; init; }
}

/* TODO: validation syntax

--- String ---

<, >, ==, !=, <=, >=

value:
- Number 
	- Value             => validate length
- String
	- Value             => validate value
	- i:Value           => validate value (case-insensitive)
	-	{Property}        => validate value against other property's value (case-sensitive)
	- {Property.case:i} => validate value against other property's value (case-insensitive) 
	- {Property.length} => validate length against other property's length

32
Hello
i:Hello
{Password}
{Password.case:i}
{Password.length}

| Value    | ExtraInfo | IsRelative  | raw_value         |
| 32       | len       | false       | <null>            |
| Hello    | <null>    | false       | <null>            |
| Hello    | c:i       | false       | i:Hello           |
| Password | <null>    | true        | {Password}        |
| Password | c:i       | true        | {Password.case:i} |
| Password | len       | true        | {Password.length} |
| [a-z0-9] | <null>    | false       | [a-z0-9]          |


*/