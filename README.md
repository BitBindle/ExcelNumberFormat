ExcelNumberFormat
=================

.NET library to parse ECMA-376 number format strings and format values like Excel and other spreadsheet softwares.

[![Build](https://github.com/andersnm/ExcelNumberFormat/actions/workflows/build.yml/badge.svg)](https://github.com/andersnm/ExcelNumberFormat/actions/workflows/build.yml)

## Install via NuGet

If you want to include ExcelNumberFormat in your project, you can [install it directly from NuGet](https://www.nuget.org/packages/ExcelNumberFormat)

To install ExcelNumberFormat, run the following command in the Package Manager Console

```
PM> Install-Package ExcelNumberFormat
```

## Usage

```C#
var format = new NumberFormat("#.##");
Console.WriteLine(format.Format(1234.56, CultureInfo.InvariantCulture));
```

A locale code such as `[$-407]` selects the culture of the month names, day names and AM/PM designators. The library looks up this culture by its id. An application can give the culture itself, for example when it runs with invariant globalization, with a reduced ICU data set, or on netstandard1.0. The resolver gets the low 16 bits of the locale code. If it returns null, the library looks up the culture:

```C#
var format = new NumberFormat("[$-407]d. mmmm yyyy");
Console.WriteLine(format.Format(44995, CultureInfo.InvariantCulture, false, localeId => localeId == 0x407 ? myGermanCulture : null));
```

`NumberFormat.DefaultLocaleCultureResolver` sets a resolver for all calls in the process, also in libraries that call `Format(value, culture)`:

```C#
NumberFormat.DefaultLocaleCultureResolver = localeId => localeId == 0x407 ? myGermanCulture : null;
```

## Features

- Parses and formats most custom number formats as expected: decimal, percent, thousands, exponential, fraction, currency, date/time, duration, text.
- Supports multiple sections with conditions.
- Formats values with relevant constants from CultureInfo.
- A locale code such as `[$-407]` selects the language of the month names, day names and AM/PM designators. The separators always come from the CultureInfo. If the language of the locale code is not known, or on netstandard1.0, the names also come from the CultureInfo.
- Supports DateTime, TimeSpan and numeric values for date and duration formats.
- Supports both 1900- and 1904-based numeric datetimes (Excel on Mac uses 1904-based dates).
- Targets net20 and netstandard1.0 for max compatibility.

## Formatting .NET types

The `Format()` method takes a value of type `object` as parameter. Internally, the value is cast or converted to a specific .NET type depending on the kind of number format:

Format Kind | Example | .NET type|Conversion strategy
-|-|-|-
Number   | 0.00      |double|Convert.ToDouble()
Fraction | 0/0       |double|Convert.ToDouble()
Exponent | \#0.0E+0  |double|Convert.ToDouble()
Date/Time| hh\:mm    |DateTime|ExcelDateTime.TryConvert()
Duration | \[hh\]\:mm|TimeSpan|Cast or TimeSpan.FromDays()
General  | General   |(any)|CompatibleConvert.ToString()
Text     | ;;;"Text: "@|string|Convert.ToString()

In case of errors, `Format()` returns the value from `CompatibleConvert.ToString()`.

`CompatibleConvert.ToString()` formats floats and doubles with explicit precision, or falls back to `Convert.ToString()` for any other types.
`ExcelDateTime.TryConvert()` uses DateTimes as is, or converts numeric values to a DateTime with adjustments for legacy Excel behaviors.

## TODO/notes

- 'General' is formatted with `.ToString()` instead of Excel conventions.
- No errors: Invalid format strings and incompatible input values are formatted with `.ToString()`.
- No color information.
- Variable width space is returned as regular space.
- Repeat-to-fill characters are printed once, not repeated.
- No alignment hinting.
- No date conditions.
