using System;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Data.Extensions;

public static class RuleOrderExtensions
{
	public static string ToDbName(this RuleOrder order) => order switch
	{
		RuleOrder.ByName => "r.name",
		RuleOrder.ByType => "r.type",
		RuleOrder.ByProperty => "p.name",
		RuleOrder.ByEndpoint => "e.name",
		_ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
	};
}