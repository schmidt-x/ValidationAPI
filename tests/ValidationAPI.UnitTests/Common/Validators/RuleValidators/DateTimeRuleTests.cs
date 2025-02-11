using ValidationAPI.Common.Models;
using ValidationAPI.Common.Validators.RuleValidators;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.UnitTests.Common.Validators.RuleValidators;

public class DateTimeRuleTests
{
	private const PropertyType DateTimeType = PropertyType.DateTime;
	
	[Fact]
	public void ShouldSucceed_DateTime()
	{
		// Arrange
		var value = TestHelpers.GetJsonProperty("\"2025-01-01T12:00:00Z\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(DateTimeType, false) { Rules = rules };
		
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
	[InlineData("noW", "+00:05")]
	[InlineData("nOw", "-00:05")]
	[InlineData("NoW", null)] // 'now' with no offset
	public void ShouldSucceed_NowWithOffset(string now, string? offset)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"\"{now}{offset}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(DateTimeType, false) { Rules = rules };
		
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
	[InlineData("+00:05")]
	[InlineData("-00:05")]
	[InlineData(null)]  // no offset
	public void ShouldSucceed_RelativeWithOffset(string? offset)
	{
		// Arrange
		const string targetPropertyName = "Username";
		var value = TestHelpers.GetJsonProperty($"\"{{{targetPropertyName}{offset}}}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(DateTimeType, false) { Rules = rules };
		
		Dictionary<string, PropertyRequest> properties = new()
		{
			{ targetPropertyName, new PropertyRequest(DateTimeType, false) }
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
	[InlineData("now-00:05", "now+00:05")]
	[InlineData("now", "now+00:05")]
	[InlineData("now-00:05", "now")]
	[InlineData("2025-01-01T12:00:00Z", "now")]
	[InlineData("2025-01-01T12:00:00Z", "now+00:05")]
	[InlineData("2025-01-01T12:00:00Z", "2025-01-01T12:00:01Z")]
	public void ShouldSucceed_Range(string lower, string upper)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"[\"{lower}\", \"{upper}\"]");
		
		RuleRequest[] rules = [ new("_", RuleType.Between, value, "") ];
		var property = new PropertyRequest(DateTimeType, false) { Rules = rules };
		
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
	[InlineData("now00:05")]
	[InlineData("{Username00:05}")]
	public void ShouldFailIfSignIsNotPresent(string input)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"\"{input}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(DateTimeType, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		// Act
		
		_ = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.False(sut.IsValid);
		Assert.Single(sut.Failures);
	}
	
	[Theory]
	[InlineData("now+00:05", "now")]
	[InlineData("2025-01-01T12:00:01Z", "2025-01-01T12:00:00Z")]
	[InlineData("now+00:05", "2026-01-01T12:00:00Z")] // «now» will exceed sooner or later
	[InlineData("now", "2026-01-01T12:00:00Z")]
	public void ShouldFailIfLowerBoundExceedsUpperBound(string lower, string upper)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"[\"{lower}\", \"{upper}\"]");
		
		RuleRequest[] rules = [ new("_", RuleType.Between, value, "") ];
		var property = new PropertyRequest(DateTimeType, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		// Act
		
		_ = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.False(sut.IsValid);
		Assert.Single(sut.Failures);
	}
	
	[Theory]
	[InlineData("NOw-00:05", "nOW+00:05")]
	[InlineData("NoW", "NOW+00:05")]
	[InlineData("nOw-00:05", "NoW")]
	public void ShouldLowerNowOptionForRange(string lower, string upper)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"[\"{lower}\", \"{upper}\"]");
		
		RuleRequest[] rules = [ new("_", RuleType.Between, value, "") ];
		var property = new PropertyRequest(DateTimeType, false) { Rules = rules };
		
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