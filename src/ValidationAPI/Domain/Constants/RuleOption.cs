﻿namespace ValidationAPI.Domain.Constants;

public static class RuleOption
{
	// string options
	public const string ByLength = ".Length";
	public const string ByLengthNormalized = ".LENGTH";
	
	public const string CaseIPostfix = ".Case:i";
	public const string CaseINormalizedPostfix = ".CASE:I";
	
	public const string CaseIPrefix = "i:";
	
	// dateTime options
	public const string Now = "now";
}