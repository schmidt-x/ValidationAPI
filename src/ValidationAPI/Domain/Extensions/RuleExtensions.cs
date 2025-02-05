using System;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Domain.Extensions;

public static class RuleExtensions
{
	public static int[] GetRangeArray(this Rule rule)
	{
		if (rule.ValueType != RuleValueType.Range)
			throw new ArgumentException(nameof(rule.ValueType));
		
		var index = int.Parse(rule.ExtraInfo!);
		return [ int.Parse(rule.Value.AsSpan(0, index)), int.Parse(rule.Value.AsSpan(index+1))];
	}
}