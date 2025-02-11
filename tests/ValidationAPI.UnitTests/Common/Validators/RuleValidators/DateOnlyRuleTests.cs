using ValidationAPI.Common.Models;
using ValidationAPI.Common.Validators.RuleValidators;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.UnitTests.Common.Validators.RuleValidators;

public class DateOnlyRuleTests
{
	private const PropertyType DateOnlyType = PropertyType.DateOnly;
	
	[Fact]
	public void ShouldSucceed()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty("\"2025-01-01\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(DateOnlyType, false) { Rules = rules };
		
		string expectedValue = value.GetString()!;
		const string? expectedRawValue = null;
		const string? expectedExtraInfo = null;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = false;
		
		var sut = new RuleValidator([]);
		
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
	[InlineData("+05:00")]
	[InlineData("-05:00")]
	[InlineData(null)]
	public void ShouldSucceed_NowWithOffset(string? offset)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"\"now{offset}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(DateOnlyType, false) { Rules = rules };
		
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
	[InlineData("+05:00")]
	[InlineData("-05:00")]
	[InlineData(null)]
	public void ShouldSucceed_RelativeWithOffset(string? offset)
	{
		// Arrange
		const string targetPropertyName = "Username";
		var value = TestHelpers.GetJsonProperty($"\"{{{targetPropertyName}{offset}}}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(DateOnlyType, false) { Rules = rules };
		
		Dictionary<string, PropertyRequest> properties = new()
		{
			{ targetPropertyName, new PropertyRequest(DateOnlyType, false) }
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
	[InlineData("now-1", "now+1")]
	[InlineData("now", "now+1")]
	[InlineData("now-1", "now")]
	[InlineData("2025-01-01", "now")]
	[InlineData("2025-01-01", "now+1")]
	[InlineData("2025-01-01", "2025-01-02")]
	public void ShouldSucceed_Range(string lower, string upper)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"[\"{lower}\", \"{upper}\"]");
		
		RuleRequest[] rules = [ new("_", RuleType.Between, value, "") ];
		var property = new PropertyRequest(DateOnlyType, false) { Rules = rules };
		
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
	[InlineData("now", "now")]
	[InlineData("2025-01-02", "2025-01-01")]
	[InlineData("now", "now-1")]
	[InlineData("now+1", "2026-01-01")] // «now» will exceed sooner or later
	[InlineData("now", "2026-01-01")]
	public void ShouldFailIfLowerBoundExceedsUpperBound(string lower, string upper)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"[\"{lower}\", \"{upper}\"]");
		
		RuleRequest[] rules = [ new("_", RuleType.Between, value, "") ];
		var property = new PropertyRequest(DateOnlyType, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		// Act
		
		_ = sut.Validate("_", "_", property);
		
		// Assert
		
		Assert.False(sut.IsValid);
		Assert.Single(sut.Failures);
	}
}