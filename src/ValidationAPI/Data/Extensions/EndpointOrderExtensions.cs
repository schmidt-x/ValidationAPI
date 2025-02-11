using System;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Data.Extensions;

public static class EndpointOrderExtensions
{
	public static string ToDbName(this EndpointOrder order) => order switch
	{
		EndpointOrder.ByName       => "name",
		EndpointOrder.ByCreatedAt  => "created_at",
		EndpointOrder.ByModifiedAt => "modified_at",
		_ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
	};
}