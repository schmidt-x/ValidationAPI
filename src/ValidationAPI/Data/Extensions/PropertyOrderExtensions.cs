using System;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Data.Extensions;

public static class PropertyOrderExtensions
{
	public static string ToDbName(this PropertyOrder order) => order switch
	{
		PropertyOrder.ByEndpoint   => "endpoint",
		PropertyOrder.ByName       => "name",
		PropertyOrder.ByType       => "type",
		PropertyOrder.ByIsOptional => "is_optional",
		PropertyOrder.ByCreatedAt  => "created_at",
		PropertyOrder.ByModifiedAt => "modified_at",
		_ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
	};
}