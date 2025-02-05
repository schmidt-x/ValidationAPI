using ValidationAPI.Common.Models;
using ValidationAPI.Common.Validators.RuleValidators;
using ValidationAPI.Domain.Constants;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.UnitTests.Common.Validators.RuleValidators;

public class StringRuleTests
{
	private const PropertyType StringType = PropertyType.String;
	
	[Fact]
	public void ShouldSucceed_Escaped()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty(""" "\\i:hello" """);
		var valueStr = value.GetString()!;
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(StringType, false) { Rules = rules };
		
		var validator = new RuleValidator([]);
		
		string expectedValue = valueStr[1..];
		string expectedRawValue = valueStr;
		string? expectedExtraInfo = null;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Fact]
	public void ShouldSucceed_ByLength()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty("3");
		const RuleType ruleType = RuleType.MoreOrEqual;
		
		RuleRequest[] rules = [ new("_", ruleType, value, "_") ];
		var property = new PropertyRequest(StringType, false) { Rules = rules };
		
		var validator = new RuleValidator([]);
		
		string expectedValue = 3.ToString();
		const string? expectedRawValue = null;
		const string expectedExtraInfo = RuleExtraInfo.ByLength;
		const RuleValueType expectedValueType = RuleValueType.Int;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Fact]
	public void ShouldSucceed_ByLengthRelative()
	{
		// Arrange
		const string targetPropName = "TargetProperty";
		var value = TestHelpers.GetJsonProperty("\"{" + targetPropName + ".Length}\""); // "{TargetProperty.Length}"
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var sourceProp = new PropertyRequest(StringType, false) { Rules = rules };
		
		Dictionary<string, PropertyRequest> requestProperties = new()
		{
			{ targetPropName , new(StringType, false) }
		};
		
		var validator = new RuleValidator(requestProperties);
		
		const string expectedValue = targetPropName;
		const string expectedRawValue = "{" + targetPropName + ".Length}";
		const string expectedExtraInfo = RuleExtraInfo.ByLength;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = true;
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", sourceProp);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Fact]
	public void ShouldSucceed_ByValue()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty("\"Hello\"");
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(PropertyType.String, false) { Rules = rules };
		
		var validator = new RuleValidator([]);
		
		string expectedValue = value.GetString()!;
		string? expectedRawValue = null;
		string? expectedExtraInfo = null;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Fact]
	public void ShouldSucceed_Relative()
	{
		// Arrange
		
		const string targetPropName = "TargetProperty";
		var value = TestHelpers.GetJsonProperty("\"{" + targetPropName + "}\""); // "{TargetProperty}"
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var sourceProp = new PropertyRequest(StringType, false) { Rules = rules };
		
		Dictionary<string, PropertyRequest> requestProperties = new()
		{
			{ targetPropName , new(StringType, false) }
		};
		
		var validator = new RuleValidator(requestProperties);
		
		const string expectedValue = targetPropName;
		const string expectedRawValue = "{" + targetPropName + "}";
		const string? expectedExtraInfo = null;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = true;
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", sourceProp);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Fact]
	public void ShouldSucceed_CaseI()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty("\"i:Hello\"");
		var valueStr = value.GetString()!;
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(PropertyType.String, false) { Rules = rules };
		
		var validator = new RuleValidator([]);
		
		string expectedValue = valueStr["i:".Length..];
		string expectedRawValue = valueStr;
		const string expectedExtraInfo = RuleExtraInfo.CaseI;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Fact]
	public void ShouldSucceed_CaseIRelative()
	{
		// Arrange
		const string targetPropName = "TargetProperty";
		var value = TestHelpers.GetJsonProperty("\"{" + targetPropName + ".case:i}\""); // "{TargetProperty.case:i}"
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var sourceProp = new PropertyRequest(StringType, false) { Rules = rules };
		
		Dictionary<string, PropertyRequest> requestProperties = new()
		{
			{ targetPropName , new(StringType, false) }
		};
		
		var validator = new RuleValidator(requestProperties);
		
		const string expectedValue = targetPropName;
		const string expectedRawValue = "{" + targetPropName + ".case:i}";
		const string expectedExtraInfo = RuleExtraInfo.CaseI;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = true;
		
		// Act
		
		var validatedRules = validator.Validate("_", "", sourceProp);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Fact]
	public void ShouldSucceed_Regex()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty("\"[a-zA-Z0-9_.]\"");
		const RuleType ruleType = RuleType.Regex;
		
		RuleRequest[] rules = [ new("", ruleType, value, "") ];
		var property = new PropertyRequest(StringType, false) { Rules = rules };
		
		var validator = new RuleValidator([]);
		
		string expectedValue = value.GetString()!;
		string? expectedRawValue = null;
		string? expectedExtraInfo = null;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Fact]
	public void ShouldSucceed_Email()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty("\"\"");
		const RuleType ruleType = RuleType.Email;
		
		RuleRequest[] rules = [ new("", ruleType, value, "") ];
		var property = new PropertyRequest(StringType, false) { Rules = rules };
		
		var validator = new RuleValidator([]);
		
		string expectedValue = string.Empty;
		string? expectedRawValue = null;
		string? expectedExtraInfo = null;
		const RuleValueType expectedValueType = RuleValueType.String;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		
		var validatedRule = validatedRules.First();
		
		Assert.Equal(expectedValue, validatedRule.Value);
		Assert.Equal(expectedRawValue, validatedRule.RawValue);
		Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
		Assert.Equal(expectedValueType, validatedRule.ValueType);
		Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
	}
	
	[Fact]
	public void ShouldSucceed_Between_Outside()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty("[ 3, 32 ]");
		
		RuleRequest[] rules = [ 
			new("_", RuleType.Between, value, ""),
			new("_", RuleType.Outside, value, "")
		];
		var property = new PropertyRequest(StringType, false) { Rules = rules };
		
		var validator = new RuleValidator([]);
		
		using var arrayEnumerator = value.EnumerateArray();
		
		arrayEnumerator.MoveNext();
		var left = arrayEnumerator.Current.GetInt32();
		arrayEnumerator.MoveNext();
		var right = arrayEnumerator.Current.GetInt32();
		
		string expectedValue = $"{left} {right}";
		string? expectedRawValue = null;
		string expectedExtraInfo = expectedValue.IndexOf(' ').ToString();
		const RuleValueType expectedValueType = RuleValueType.Range;
		const bool expectedIsRelative = false;
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", property);
		
		// Assert
		
		Assert.True(validator.IsValid);
		Assert.NotNull(validatedRules);
		Assert.Equal(rules.Length, validatedRules.Count);
		
		foreach (var validatedRule in validatedRules)
		{
			Assert.Equal(expectedValue, validatedRule.Value);
			Assert.Equal(expectedRawValue, validatedRule.RawValue);
			Assert.Equal(expectedExtraInfo, validatedRule.ExtraInfo);
			Assert.Equal(expectedValueType, validatedRule.ValueType);
			Assert.Equal(expectedIsRelative, validatedRule.IsRelative);
		}
	}
	
	[Fact]
	public void ShouldFailIfEscapedValueTooShort()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty(""" "\\" """);
		
		RuleRequest[] rules = [ new("_", RuleType.Equal, value, "") ];
		var property = new PropertyRequest(StringType, false) { Rules = rules };
		
		var validator = new RuleValidator([]);
		
		const string expectedErrorCode = ErrorCodes.EMPTY_RULE_VALUE;
		string expectedErrorMessage = $"[{rules[0].Name}] Empty value.";
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", property);
		
		// Assert
		
		Assert.False(validator.IsValid);
		Assert.Null(validatedRules);
		
		var failure = validator.Failures.First().Value.First();
		
		Assert.Equal(expectedErrorCode, failure.Code);
		Assert.Equal(expectedErrorMessage, failure.Message);
	}
	
	[Fact]
	public void ShouldReturnNullIfFailed()
	{
		// Arrange
		
		var value = TestHelpers.GetJsonProperty("\"\""); // will fail on empty string within Comparison or Regex rules
		
		RuleRequest[] rules = [ new("_", RuleType.Regex, value, "") ];
		var property = new PropertyRequest(StringType, false) { Rules = rules };
		
		var validator = new RuleValidator([]);
		
		// Act
		
		var validatedRules = validator.Validate("_", "_", property);
		
		// Assert
		
		Assert.False(validator.IsValid);
		Assert.Null(validatedRules);
		Assert.Single(validator.Failures);
	}
}