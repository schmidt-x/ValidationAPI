using System;
namespace ValidationAPI.Domain.Enums;

public enum PropertyOrder
{
	ByEndpoint,
	ByName,
	ByType,
	ByCreatedAt,
	ByModifiedAt
}

public static class PropertyOrderExtensions
{
	public static string ToDbName(this PropertyOrder order) => order switch
	{
		PropertyOrder.ByEndpoint   => "endpoint",
		PropertyOrder.ByName       => "name",
		PropertyOrder.ByType       => "type",
		PropertyOrder.ByCreatedAt  => "created_at",
		PropertyOrder.ByModifiedAt => "modified_at",
		_ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
	};
}