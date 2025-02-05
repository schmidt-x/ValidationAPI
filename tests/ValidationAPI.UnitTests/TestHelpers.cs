using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ValidationAPI.UnitTests;

public static class TestHelpers
{
	public static JsonElement GetJsonProperty([StringSyntax("Json")] string value)
		=> JsonDocument.Parse(value).RootElement;
}