using System;
using System.Collections.Generic;
using System.Text.Json;
using ValidationAPI.Common.Extensions;
using ValidationAPI.Common.Models;
using ValidationAPI.Domain.Entities;
using ValidationAPI.Domain.Enums;
using ValidationAPI.Features.Validation.Models;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Features.Validation.Validators;

public static partial class PropertyValidators
{
	public static Dictionary<string, UnvalidatedProperty>? ValidateTypes(
		List<Property> dbProperties,
		Dictionary<string, JsonElement> requestProperties,
		Dictionary<string, List<ErrorDetail>> failures)
	{
		Dictionary<string, UnvalidatedProperty> unvalidatedProperties = [];
		
		foreach (var dbProperty in dbProperties)
		{
			if (!requestProperties.TryGetValue(dbProperty.Name, out var requestValue))
			{
				if (!dbProperty.IsOptional)
				{
					failures.AddErrorDetail(dbProperty.Name, PROPERTY_NOT_PRESENT,
						$"Property is not present (type '{dbProperty.Type}'). Consider making it 'optional'.");
				}
				continue;
			}
			
			if (dbProperty.Type is PropertyType.Int or PropertyType.Float)
			{
				if (requestValue.ValueKind != JsonValueKind.Number)
				{
					failures.AddErrorDetail(
						dbProperty.Name, INVALID_PROPERTY_TYPE, 
						$"Expected value kind is 'Json.Number'; got: 'Json.{requestValue.ValueKind}'.");
					continue;
				} 
			}
			else
			{
				if (requestValue.ValueKind != JsonValueKind.String)
				{
					failures.AddErrorDetail(
						dbProperty.Name, INVALID_PROPERTY_TYPE,
						$"Expected value kind is 'Json.String'; got: 'Json.{requestValue.ValueKind}'.");
					continue;
				}
			}
			
			object value;
			
			switch (dbProperty.Type)
			{
				case PropertyType.Int:
					if (!requestValue.TryGetInt64(out long intValue))
					{
						failures.AddErrorDetail(dbProperty.Name, INVALID_PROPERTY_TYPE, "Value is not valid Int.");
						continue;
					}
					value = intValue;
					break;
				
				case PropertyType.Float:
					if (!requestValue.TryGetDouble(out double floatValue))
					{
						failures.AddErrorDetail(dbProperty.Name, INVALID_PROPERTY_TYPE, "Value is not valid Float.");
						continue;
					}
					value = floatValue;
					break;
				
				case PropertyType.String:
					value = requestValue.GetString()!;
					break;
				
				case PropertyType.DateTime:
					if (!DateTimeOffset.TryParse(requestValue.GetString()!, out var dt))
					{
						failures.AddErrorDetail(dbProperty.Name, INVALID_PROPERTY_TYPE, "Value is not valid DateTime.");
						continue;
					}
					value = dt;
					break;
				
				case PropertyType.DateOnly:
				case PropertyType.TimeOnly:
					throw new NotImplementedException();
				
				default:
					throw new ArgumentOutOfRangeException(nameof(dbProperties));
			}
			
			if (failures.Count != 0) continue;
			
			unvalidatedProperties.Add(
				dbProperty.Name, new UnvalidatedProperty(dbProperty.Id, dbProperty.Name, dbProperty.Type, value));
		}
		
		return failures.Count == 0 ? unvalidatedProperties : null;
	}
}