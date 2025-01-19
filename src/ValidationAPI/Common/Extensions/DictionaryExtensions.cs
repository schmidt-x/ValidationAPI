using System.Collections.Generic;
using ValidationAPI.Common.Models;

namespace ValidationAPI.Common.Extensions;

public static class DictionaryExtensions
{
	public static void AddErrorDetail(
		this Dictionary<string, List<ErrorDetail>> errors, string key, string code, string errorMessage)
	{
		if (errors.TryGetValue(key, out var details))
		{
			details.Add(new ErrorDetail(code, errorMessage));
		}
		else
		{
			errors.Add(key, [ new ErrorDetail(code, errorMessage) ]);
		}
	}
}