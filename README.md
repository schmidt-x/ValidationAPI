# Validation API

This API provides a flexible and dynamic approach to user-input validation by allowing developers to define custom validation rules, request body structures, and endpoints on the fly. Instead of modifying validation code whenever business rules change, developers can update validation configurations dynamically, ensuring adaptability and reducing maintenance overhead. This approach centralizes validation logic and promotes consistency across services.

# Table of Contents
- [Usage](#usage)
  - [Defining Endpoint](#defining-endpoint)
  - [Validating Request](#validating-request)
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
      - [Offset](#offset)
  - [IsOptional](#isoptional)


## Usage

### Defining Endpoint

The following request defines an endpoint `/my-endpoint` with the following properties and rules:
- EmailAddress (String)
  - Must be a valid email address
- Username (String)
  - Length must be between 3 and 32 (inclusive)
  - Can only contain lower (a-z), upper (A-Z), underscores (_), or periods
- DateOfBirth (DateOnly)
  - User must be at least 18 years old (6,574 days approximately)
- Password (String)
  - Length must be at least 8 characters
- ConfirmPassword (String)
  - Must be exactly equal to **Password** (case-sensitive)
- OldPassword (String)
  - *(rules omitted for brevity)*
- NewPassword (String)
  - Must **not** be identical to **OldPassword** (case-insensitive)
- StartTime (DateTime)
  - Must be at least 1 minute earlier than the time the request is made, and no earlier than January 1, 2025
- EndTime (DateTime)
  - Must be at least 5 minutes later than **StartTime**
  - Must be within the year 2025

```cs
POST /api/endpoints
{
  "Endpoint": "my-endpoint",
  "Description": "My description.",
  "Properties": {
    "EmailAddress": {
      "Type": "String",
      "IsOptional": false,
      "Rules": [
        {
          "Name": "INVALID_EMAIL_ADDRESS",
          "Type": "Email",
          "Value": "",
          "ErrorMessage": "Value '{actualValue}' is not a valid email address."
        }
      ]
    },
    "Username": {
      "Type": "String",
      "IsOptional": false,
      "Rules": [
        {
          "Name": "USERNAME_MIN_MAX_LENGTH",
          "Type": "Between",
          "Value": [3, 32],
          "ErrorMessage": "Username length must be between {value1} and {value2} (inculsive); got: {actualValue}."
        },
        {
          "Name": "USERNAME_BAD_CHARS",
          "Type": "Regex",
          "Value": "^[a-zA-Z0-9_.]+$",
          "ErrorMessage": "Username can only contain letters (a-z, A-Z), digits (0-9), underscores (_), or periods."
        }
      ]
    },
    "DateOfBirth": {
      "Type": "DateOnly",
      "IsOptional": false,
      "Rules": [
        {
          "Name": "DATE_OF_BIRTH_MINOR",
          "Type": "<=",
          "Value": "now-6574",
          "ErrorMessage": "You must be an adult."
        }
      ]
    },
    "Password": {
      "Type": "String",
      "IsOptional": false,
      "Rules": [
        {
          "Name": "PASSWORD_MIN_LENGTH",
          "Type": ">=",
          "Value": 8,
          "ErrorMessage": "Password must be at least {value} characters long; got: {actualValue}."
        }
      ]
    },
    "ConfirmPassword": {
      "Type": "String",
      "IsOptional": false,
      "Rules": [
        {
          "Name": "PASSWORDS_EQUALITY",
          "Type": "==",
          "Value": "{Password}",
          "ErrorMessage": "Passwords must match."
        }
      ]
    },
    "OldPassword": {
      "Type": "String",
      "IsOptional": false,
      "Rules": []
    },
    "NewPassword": {
      "Type": "String",
      "IsOptional": false,
      "Rules": [
        {
          "Name": "NEW_PASSWORD_EQUALITY",
          "Type": "!=",
          "Value": "{OldPassword.Case:i}",
          "ErrorMessage": "New password must not be equal to old password (case-insensitive)."
        }
      ]
    },
    "StartTime": {
      "Type": "DateTime",
      "IsOptional": false,
      "Rules": [
        {
          "Name": "START_TIME_RANGE",
          "Type": "Between",
          "Value": [ "2025-01-01T00:00:00Z", "now-00:01" ],
          "ErrorMessage": "Start time must be at least 1 minute in the past and no earlier than January 1, 2025."
        }
      ]
    },
    "EndTime": {
      "Type": "DateTime",
      "IsOptional": false,
      "Rules": [
        {
          "Name": "END_TIME_OFFSET",
          "Type": ">=",
          "Value": "{StartTime+00:05}",
          "ErrorMessage": "End time must be at least 5 minutes after Start time."
        },
        {
          "Name": "END_TIME_WITHIN_THE_YEAR",
          "Type": "<",
          "Value": "2025-12-31T23:59:59Z",
          "ErrorMessage": "End time must be within this year (2025)."
        }
      ]
    }
  }
}
```

### Validating Request

Request:
```cs
POST /api/validate/my-endpoint
{
  "EmailAddress": "invalid-value",
  "Username": "a$",
  "DateOfBirth": "2010-01-01",
  "Password": "foo",
  "ConfirmPassword": "bar",
  "OldPassword": "qwerty",
  "NewPassword": "QWERTY",
  "StartTime": "2026-01-01T00:00:00Z",
  "EndTime": "2026-01-01T00:01:00Z"
}
```

Response:
```cs
HTTP/1.1 200 OK
{
  "Status": "FAILURE",
  "ProcessedProperties": 9,
  "AppliedRules": 10,
  "Failures": {
    "EmailAddress": [
      {
        "Code": "INVALID_EMAIL_ADDRESS",
        "Message": "Value 'invalid_email' is not a valid email address."
      }
    ],
    "Username": [
      {
        "Code": "USERNAME_MIN_MAX_LENGTH",
        "Message": "Username length must be between 3 and 32 (inculsive); got: 2."
      },
      {
        "Code": "USERNAME_BAD_CHARS",
        "Message": "Username can only contain letters (a-z, A-Z), digits (0-9), underscores (_), or periods."
      }
    ],
    "DateOfBirth": [
      {
        "Code": "DATE_OF_BIRTH_MINOR",
        "Message": "You must be an adult."
      }
    ],
    "Password": [
      {
        "Code": "PASSWORD_MIN_LENGTH",
        "Message": "Password must be at least 8 characters long; got: 3."
      }
    ],
    "ConfirmPassword": [
      {
        "Code": "PASSWORDS_EQUALITY",
        "Message": "Passwords must match."
      }
    ],
    "NewPassword": [
      {
        "Code": "NEW_PASSWORD_EQUALITY",
        "Message": "New password must not be equal to old password (case-insensitive)."
      }
    ],
    "StartTime": [
      {
        "Code": "START_TIME_RANGE",
        "Message": "Start time must be at least 1 minute in the past and no earlier than January 1, 2025."
      }
    ],
    "EndTime": [
      {
        "Code": "END_TIME_START_TIME_OFFSET",
        "Message": "End time must be at least 5 minutes after Start time."
      },
      {
        "Code": "END_TIME_WITHIN_THE_YEAR",
        "Message": "End time must be within this year (2025)."
      }
    ]
  }
}
```

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

#### Range-Based Comparison Rules (multi-value comparisons)
  - `Between` (must fall **within** two values, including the boundaries)
  - `Outside` (must be **either less than** the lower bound **or greater than** the upper bound)

#### String-Specific Rules
  - `Regex` ([Regular Expression Language - Quick Reference](https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference))
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
> Relative rules are only supported for [Comparison](#comparison-rules-single-value-comparisons) types.<br>
> For details on specific behavior, refer to the definition of each [Property Type](#type-1).

### ErrorMessage

Returned alongside **[Name](#name)** to provide additional information or guidance when a rule fails. It supports a few placeholders, which are dynamically replaced with relevant values:
- `{value}` -> replaced with the expected value defined in the rule.
- `{value1}` and `{value2}` -> replaced with the lower and upper bounds in the [Range-Based Comparison](#range-based-comparison-rules-multi-value-comparisons) rules.
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
> 
> Placeholders are case-insensitive.

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

**Int** represents a signed 64-bit integer, ranging from **-9,223,372,036,854,775,808** to **9,223,372,036,854,775,807**.

**Float** represents a signed 64-bit floating-point number, with an approximate range of **±5.0 × 10⁻³²⁴** to **±1.7 × 10³⁰⁸**, and a precision of **~15-17 digits**.

Both support [Comparison](#comparison-rules-single-value-comparisons) (including [Relative rules](#relative-rules)) and [Range-Based Comparison](#range-based-comparison-rules-multi-value-comparisons) rules.

#### String

[Comparison](#comparison-rules-single-value-comparisons) rules for strings support three variants:
1. **By Value** (case-sensitive) — Compares the string exactly as it is.
2. **By Value** (case-insensitive) — Compares the string without considering the letter case.
3. **By Length** — Compares the string based on its character count.

To compare a string **by value** in a case-sensitive manner, rule [Value](#value) must be a **Json.String**.<br>
For a **case-insensitive** comparison, prepend `"i:"` to the **Json.String** value (e.g., `"Value": "i:example"`).<br>
To compare **by length**, [Value](#value) must be a **Json.Number** (e.g., `"Value": 8`).

> [!NOTE]
> If the actual string value needs to start with `"i:"` without triggering case-insensitive mode, escape it by prepending `"\\"` (e.g., `"Value": "\\i:example"`).

The same behavior can be achieved for [relative rules](#relative-rules) by appending **options**:

- `{PropertyName}` — Default, case-sensitive comparison.
- `.Case:i` — Enables case-insensitive comparison (e.g., `"Value": "{PropertyName.Case:i}"`).
- `.Length` — Compares two strings **by length** (e.g., `"Value": "{PropertyName.Length}"`).

> [!NOTE]
> If the actual string value needs to start with `"{"` without being treated as a **relative rule**, escape it by prepending `"\\"` (e.g., `"Value": "\\{example}"`).
> 
> **Option** names are case-insensitive.

[Range-Based Comparison](#range-based-comparison-rules-multi-value-comparisons) rules are also supported, but only for comparison **by length**:

```json
{
  "Name": "USERNAME_MIN_MAX_LENGTH",
  "Type": "Between",
  "Value": [3, 32],
  "ErrorMessage": "Username length must be between {value1} and {value2} (inclusive); got: {actualValue}."
},
{
  "Name": "EXAMPLE",
  "Type": "Outside",
  "Value": [10, 20],
  "ErrorMessage": "Length must be either less than {value1} or greater than {value2}; got: {actualValue}."
}
```

[String-Specific](#string-specific-rules) rules (i.e., `Regex` and `Email`) are pretty self-explanatory.

#### DateTime / DateOnly / TimeOnly

Type | Description
---|---
DateTime | Represents dates and times with values whose UTC ranges from 12:00:00 midnight, January 1, 0001 Anno Domini (Common Era), to 11:59:59 P.M., December 31, 9999 A.D. (C.E.).<br>`2025-01-01T12:00:00+03:00` in **ISO 8061** format. 
DateOnly | Represents dates with values ranging from January 1, 0001 Anno Domini (Common Era) through December 31, 9999 A.D. (C.E.) in the Gregorian calendar.<br>`2025-01-01` 
TimeOnly | Represents a time of day, as would be read from a clock, within the range 00:00:00 to 23:59:59.9999999.<br>`12:00:00`

All support [Comparison](#comparison-rules-single-value-comparisons) (including [Relative rules](#relative-rules)) and [Range-Based Comparison](#range-based-comparison-rules-multi-value-comparisons) rules.

For **Date**/**Time** properties, you can define fixed rule values or dynamically reference the current UTC time (at the moment of validation) using the keyword `now`.

For example, to ensure that the **DateTime** property (named `StartTime`) is in the past, you would define the following rule:

```json
{
  "Name": "START_TIME_IN_FUTURE",
  "Type": "<",
  "Value": "now",
  "ErrorMessage": "Start time must be in the past."
}
```

##### Offset

You can optionally add an **offset** to the current time or to the [referenced](#relative-rules) property values:
- `now[offset]` — adds an offset to the current UTC time
- `{TargetProperty[offset]}` — adds an offset to the referenced property's value

Offset format: `{ + | - }{ d | [d.]hh:mm[:ss] }`

Elements in square brackets (`[` and `]`) are optional. One selection from the list of alternatives enclosed in braces (`{` and `}`) and separated by vertical bars (|) is required. The following table describes each element:

| Element | Description                                  |
|---------|----------------------------------------------|
| +       | Plus sign, which indicates positive offset.  |
| -       | Minus sign, which indicates negative offset. |
| d       | Days, ranging from 0 to 10675199.            |
| .       | A symbol that separates days from hours.     |
| hh      | Hours, ranging from 0 to 23.                 |
| :       | Time separator symbol.                       |
| mm      | Minutes, ranging from 0 to 59.               |
| ss      | Seconds, ranging from 0 to 59.               |

Example:
```json
{
  "Name": "RULE_1",
  "Type": "Between",
  "Value": [ "2025-01-01", "now-01:00" ],
  "ErrorMessage": "Value must be at least an hour in the past and no earlier than January 1, 2025."
},
{
  "Name": "RULE_2",
  "Type": ">=",
  "Value": "{StartTime+00:01}",
  "ErrorMessage": "End time must be at least one minute after the Start time."
}
```

### IsOptional

Specifies whether a property is required or can be omitted from the request body. If marked as **optional** and absent from the request, validation is skipped.

> [!NOTE]
> Optional properties have a limitation — rules cannot [reference](#relative-rules) them.

