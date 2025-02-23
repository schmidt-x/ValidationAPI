using ValidationAPI.Common.Models;
using ValidationAPI.Common.Validators.RuleValidators;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.UnitTests.Common.Validators.RuleValidators;

public class NumberRuleTests
{
	[Theory]
	[InlineData("42", PropertyType.Int, RuleValueType.Int)]
	[InlineData("42.5", PropertyType.Float, RuleValueType.Float)]
	public void ShouldSucceed_Number(string input, PropertyType propType, RuleValueType valueType)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"{input}");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(propType, false) { Rules = rules };
		
		var sut = new RuleValidator([]);
		
		string expectedValue = value.GetRawText();
		const string? expectedRawValue = null;
		const string? expectedExtraInfo = null;
		RuleValueType expectedValueType = valueType;
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
	[InlineData(PropertyType.Int)]
	[InlineData(PropertyType.Float)]
	public void ShouldSucceed_Relative(PropertyType type)
	{
		// Arrange
		
		const string targetPropertyName = "Hello_There";
		var value = TestHelpers.GetJsonProperty($"\"{{{targetPropertyName}}}\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(type, false) { Rules = rules };
		
		Dictionary<string, PropertyRequest> properties = new()
		{
			{ targetPropertyName, new PropertyRequest(type, false) }
		};
		
		var sut = new RuleValidator(properties);
		
		const string expectedValue = targetPropertyName;
		string expectedRawValue = value.GetString()!;
		string? expectedExtraInfo = null;
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
	[InlineData("3", "32", PropertyType.Int)]
	[InlineData("3.5", "32.5", PropertyType.Float)]
	public void ShouldSucceed_Range(string lower, string upper, PropertyType type)
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty($"[ {lower}, {upper} ]");
		
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
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
}