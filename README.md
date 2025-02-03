# Validation API

This API provides a flexible and dynamic approach to user-input validation by allowing developers to define custom validation rules, request body structures, and endpoints on the fly. Instead of hardcoding validation logic across multiple services, this centralized solution ensures consistency, scalability, and ease of maintenance.

# Table of Contents
- [Usage](#usage)
- [Rules](#rules)
  - [Name](#name)
  - [Type](#type)
    - [Comparison Rules (single-value comparisons)](#comparison-rules-single-value-comparisons)
    - [Range-Based comparison (multi-value comparisons)](#range-based-comparison-rules-multi-value-comparisons)
    - [String-Specific Rules](#string-specific-rules)
  - [Value](#value)
    - [Relative Rules](#relative-rules)
  - [ErrorMessage](#errormessage)
- [Properties](#properties)
  - [Name](#name-1)
  - [Type](#type-1)
    - [Int and Float](#int-and-float)
    - [String](#string)
    - [DateTime / DateOnly / TimeOnly](#datetime--dateonly--timeonly)
  - [IsOptional](#isoptional)


## Usage

TODO

## Rules

```json
{
  "Name": "PASSWORD_LOWERCASE",
  "Type": "Regex",
  "Value": "[a-z]",
  "ErrorMessage": "Password must contain at least one lowercase (a-z)."
}
```

### Name
Rule name (case-insensitive), unique to each endpoint.<br>
When the rule fails, **Name** is returned along with the **[ErrorMessage](#errormessage)**.

### Type

Rules are categorized into three groups:
#### Comparison Rules (single-value comparisons)
  - `<` (Less)
  - `>` (More)
  - `<=` (LessOrEqual)
  - `>=` (MoreOrEqual)
  - `==` (Equal)
  - `!=` (NotEqual)

#### Range-Based comparison Rules (multi-value comparisons)
  - `Between` (must fall within two values, including the boundaries)
  - `Outside` (must be **either less than** the lower bound **or greater than** the upper bound)

#### String-Specific Rules
  - `Regex`
  - `Email`

> [!NOTE]
> **Comparison** and **Range-Based Comparison** rules can be applied to any [Property Type](#type-1), whereas **String-Specific** rules apply only to [String](#string) properties.

### Value

The **Value** field can have different JSON kinds based on the rule type:
- Number
- String
- Array

Additionally, the behavior of a rule may vary depending on the property type it is applied to. For example, when the **Value** is of type **Number** and the rule is applied to a **[String](#string)** property, the validation checks the string's length rather than its content:

```json
{
  "Name": "LENGTH_EQUALITY",
  "Type": "==",
  "Value": 8,
  "ErrorMessage": "String's length must be 8"
},
{
  "Name": "VALUE_EQUALITY",
  "Type": "==",
  "Value": "8",
  "ErrorMessage": "String must be equal to '8'"
}
```

> [!IMPORTANT]
> Refer to the definition of each [Property Type](#type-1) for the specific rule behavior.

#### Relative rules

In addition to validating request properties against predefined values, you can also validate them **against each other** by specifying the **property name** as a **value** enclosed in curly braces: `"Value": "{PropertyName}"`.

Given two properties, **OldPassword** and **NewPassword**, the latter would have the following rule:

```json
{
  "Name": "NEW_PASSWORD_EQUALITY",
  "Type": "!=",
  "Value": "{OldPassword}",
  "ErrorMessage": "New password cannot be the same as old password."
}
```

> [!NOTE]
> Relative rules are only supported for **Comparison** and **Range-Based Comparison** types.<br>
> For details on specific behavior, refer to the definition of each [Property Type](#type-1).

### ErrorMessage

Returned alongside **[Name](#name)** to provide additional information or guidance when a rule fails. It supports two placeholders: `{value}` and `{actualValue}` (both case-insensitive), which are dynamically replaced with relevant values:
- `{value}` -> replaced with the expected value defined in the rule.
- `{actualValue}` -> replaced with the actual value received in the request.

Example:
```json
{
  "Name": "USERNAME_MIN_LENGTH",
  "Type": ">=",
  "Value": 3, 
  "ErrorMessage": "Username must be at least {value} characters long; got {actualValue}."
}
```
If a request fails this rule (e.g., the username provided is only 2 characters long), the error response would contain the following error object:

```json
{
  "Code": "USERNAME_MIN_LENGTH",
  "ErrorMessage": "Username must be at least 3 characters long; got 2."
}
```

> [!NOTE]
> If the rule is **[relative](#relative-rules)**, `{value}` is replaced with the name of the referenced property.

## Properties

```json
{
  "Name": "Age",
  "Type": "Int",
  "IsOptional": false,
  "Rules": []
}
```

### Name

Property name (case-sensitive), unique to each endpoint.

### Type

Supported property types:
- Int
- Float
- String
- DateTime
- DateOnly
- TimeOnly

When a validation request is sent, the following mappings apply to value types in the request body:
- Json.Number -> Int, Float
- Json.String -> String, DateTime, DateOnly, TimeOnly

> **Json.Null** is treated as a separate type and is therefore not considered a valid value type.

#### Int and Float

These types support both [Comparison](#comparison-rules-single-value-comparisons) and [Range-Based Comparison](#range-based-comparison-rules-multi-value-comparisons), including [relative rules](#relative-rules).

#### String

[Comparison](#comparison-rules-single-value-comparisons) rules for strings support three variants:
1. **By Value** (case-sensitive) — Compares the string exactly as it is.
2. **By Value** (case-insensitive) — Compares the string without considering the letter case.
3. **By Length** — Compares the string based on its character count.

To compare a string **by value** in a case-sensitive manner, rule [Value](#value) must be a **Json.String**.

For a **case-insensitive** comparison, prepend `"i:"` to the **Json.String** value (e.g., `"Value": "i:example"`).

To compare **by length**, [Value](#value) must be a **Json.Number** (e.g., `"Value": 8`).

> [!NOTE]
> If the actual string value needs to start with `"i:"` without triggering case-insensitive mode, escape it by prepending `"\\"` (e.g., `"Value": "\\i:example"`).

The same behavior can be achieved for [relative rules](#relative-rules) by appending **options**:

- `{PropertyName}` — Default, case-sensitive comparison.
- `.Case:i` — Enables case-insensitive comparison (e.g., `"Value": "{PropertyName.Case:i}"`).
- `.Length` — Compares two strings **by length** (e.g., `"Value": "{PropertyName.Length}"`).

> [!NOTE]
> If the actual string value needs to start with `"{"` without being treated as a **relative rule**, escape it by prepending `"\\"` (e.g., `"Value": "\\{example}"`).<br>
> **Option** names are case-insensitive.

[Range-Based Comparison](#range-based-comparison-rules-multi-value-comparisons) rules are also supported, but only for comparison **by length**:

```json
{
  "Name": "USERNAME_MIN_MAX_LENGTH",
  "Type": "Between",
  "Value": [3, 32],
  "ErrorMessage": "Username length must be between 3 and 32 (inclusive); got: {actualValue}."
},
{
  "Name": "EXAMPLE",
  "Type": "Outside",
  "Value": [10, 20],
  "ErrorMessage": "Length must be either less than 10 or greater than 20; got: {actualValue}."
}
```

[String-Specific](#string-specific-rules) rules (i.e., `Regex` and `Email`) are pretty self-explanatory.

#### DateTime / DateOnly / TimeOnly

TODO

### IsOptional

Determines whether a property is required or can be omitted from the request body. If a property is marked as **optional**, it does not need to be included in the request.

> [!NOTE]
> Optional properties have a limitation — rules cannot [reference](#relative-rules) them.

