using ValidationAPI.Common.Models;
using ValidationAPI.Common.Validators.RuleValidators;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.UnitTests.Common.Validators.RuleValidators;

public class DateTimeRuleTests
{
	[Theory]
	[InlineData("2025-01-01T12:00:00Z", PropertyType.DateTime)]
	[InlineData("2025-01-01", PropertyType.DateOnly)]
	public void ShouldSucceed_DateTime(string date, PropertyType type)
	{
		// Arrange
		var value = TestHelpers.GetJsonProperty($"\"{date}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(type, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		string expectedValue = value.GetString()!;
		const string? expectedRawValue = null;
		const string? expectedExtraInfo = null;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(sut.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Theory]
	[InlineData("noW", "+00:05", PropertyType.DateTime)]
	[InlineData("nOw", "-00:05", PropertyType.DateTime)]
	[InlineData("NoW", null, PropertyType.DateTime)]
	[InlineData("NOW", "+1", PropertyType.DateOnly)]
	[InlineData("now", "-1", PropertyType.DateOnly)]
	[InlineData("NOw", null, PropertyType.DateOnly)]
	public void ShouldSucceed_NowWithOffset(string now, string? offset, PropertyType type)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"\"{now}{offset}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(type, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		const string expectedValue = RuleOption.Now;
		string? expectedRawValue = offset is null ? null : value.GetString();
		string? expectedExtraInfo = offset is null ? null : offset.StartsWith('+') ? offset[1..] : offset;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(sut.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Theory]
	[InlineData("+00:05", PropertyType.DateTime)]
	[InlineData("-00:05", PropertyType.DateTime)]
	[InlineData(null, PropertyType.DateTime)]  // no offset
	[InlineData("+1", PropertyType.DateOnly)]
	[InlineData("-1", PropertyType.DateOnly)]
	[InlineData(null, PropertyType.DateOnly)]  // no offset
	public void ShouldSucceed_RelativeWithOffset(string? offset, PropertyType type)
	{
		// Arrange
		const string targetPropertyName = "Hello_There";
		var value = TestHelpers.GetJsonProperty($"\"{{{targetPropertyName}{offset}}}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(type, false) { Rules = rules };
		
		Dictionary<string, PropertyRequest> properties = new()
		{
			{ targetPropertyName, new PropertyRequest(type, false) }
		};
		
		var sut = new RuleValidator(properties);
		
		string expectedValue = targetPropertyName;
		string expectedRawValue = value.GetString()!;
		string? expectedExtraInfo = offset is null ? null : offset.StartsWith('+') ? offset[1..] : offset;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = true;
		
		// Act
		
		var validatedRules = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(sut.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Theory]
	[InlineData("now-00:05", "now+00:05", PropertyType.DateTime)]
	[InlineData("now", "now+00:05", PropertyType.DateTime)]
	[InlineData("now-00:05", "now", PropertyType.DateTime)]
	[InlineData("2025-01-01T12:00:00Z", "now", PropertyType.DateTime)]
	[InlineData("2025-01-01T12:00:00Z", "now+00:05", PropertyType.DateTime)]
	[InlineData("2025-01-01T12:00:00Z", "2025-01-01T12:00:01Z", PropertyType.DateTime)]
	[InlineData("now-1", "now+1", PropertyType.DateOnly)]
	[InlineData("now", "now+1", PropertyType.DateOnly)]
	[InlineData("now-1", "now", PropertyType.DateOnly)]
	[InlineData("2025-01-01", "now", PropertyType.DateOnly)]
	[InlineData("2025-01-01", "now+1", PropertyType.DateOnly)]
	[InlineData("2025-01-01", "2025-01-02", PropertyType.DateOnly)]
	public void ShouldSucceed_Range(string lower, string upper, PropertyType type)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"[\"{lower}\", \"{upper}\"]");
		
		RuleRequest[] rules = [ new("_", RuleType.Between, value, "") ];
		var property = new PropertyRequest(type, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		string expectedValue = lower;
		string expectedRawValue = value.GetRawText();
		string expectedExtraInfo = upper;
		const RuleValueType expectedValueType = RuleValueType.Range;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(sut.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.Single();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Theory]
	[InlineData("now00:05", PropertyType.DateTime)]
	[InlineData("{Username00:05}", PropertyType.DateTime)]
	[InlineData("now1", PropertyType.DateOnly)]
	[InlineData("{Username1.00:00}", PropertyType.DateOnly)]
	public void ShouldFailIfSignIsNotPresent(string input, PropertyType type)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"\"{input}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(type, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		// Act
		
		_ = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.False(sut.IsValid);
		Assert.Single(sut.Failures);
	}
	
	[Theory]
	[InlineData("now+00:05", "now", PropertyType.DateTime)]
	[InlineData("2025-01-01T12:00:01Z", "2025-01-01T12:00:00Z", PropertyType.DateTime)]
	[InlineData("now", "2026-01-01T12:00:00Z", PropertyType.DateTime)] // «now» will exceed sooner or later
	// time component should be ignored for DateOnly, e.g., now[+-anyTimeOffset] should still be 'now'
	[InlineData("now-23:59", "now", PropertyType.DateOnly)]
	[InlineData("2025-01-01", "2025-01-01", PropertyType.DateOnly)]
	[InlineData("now", "2026-01-01", PropertyType.DateOnly)]
	public void ShouldFailIfLowerBoundExceedsUpperBound(string lower, string upper, PropertyType type)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"[\"{lower}\", \"{upper}\"]");
		
		RuleRequest[] rules = [ new("_", RuleType.Between, value, "") ];
		var property = new PropertyRequest(type, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		// Act
		
		_ = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.False(sut.IsValid);
		Assert.Single(sut.Failures);
	}
	
	[Theory]
	[InlineData("NOw-00:05", "nOW+00:05", PropertyType.DateTime)]
	[InlineData("nOw-1", "NoW", PropertyType.DateOnly)]
	public void ShouldLowerNowOptionForRange(string lower, string upper, PropertyType type)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"[\"{lower}\", \"{upper}\"]");
		
		RuleRequest[] rules = [ new("_", RuleType.Between, value, "") ];
		var property = new PropertyRequest(type, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		string expectedValue = RuleOption.Now + lower[RuleOption.Now.Length..];
		string expectedExtraInfo = RuleOption.Now + upper[RuleOption.Now.Length..];
		
		// Act
		
		var validatedRules = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(sut.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.Single();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
	}
}