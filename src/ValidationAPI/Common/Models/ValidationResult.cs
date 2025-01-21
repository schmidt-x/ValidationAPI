using System.Collections.Generic;

namespace ValidationAPI.Common.Models;

public class ValidationResult
{
	public string Status { get; init; }
	public int ProcessedProperties { get; init; }
	public int AppliedRules { get; init; }
	public Dictionary<string, List<ErrorDetail>> Failures { get; init; }
	
	private ValidationResult(
		string status, int processedProperties, int appliedRules, Dictionary<string, List<ErrorDetail>> failures)
	{
		Status = status;
		ProcessedProperties = processedProperties;
		AppliedRules = appliedRules;
		Failures = failures;
	}
	
	public static ValidationResult Failure(
		int propertiesProcessed,
		int rulesApplied,
		Dictionary<string, List<ErrorDetail>> failures)
	{
		return new ValidationResult("FAILURE", propertiesProcessed, rulesApplied, failures);
	}
	
	public static ValidationResult Success(int propertiesProcessed, int rulesApplied)
	{
		return new ValidationResult("SUCCESS", propertiesProcessed, rulesApplied, []);
	}
}