ExcelNumberFormat
=================

.NET library to parse ECMA-376 number format strings and format values like Excel and other spreadsheet softwares.

[![Build](https://github.com/andersnm/ExcelNumberFormat/actions/workflows/ci.yml/badge.svg)](https://github.com/andersnm/ExcelNumberFormat/actions/workflows/ci.yml)

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

A locale code such as `[$-407]` selects the culture of the month names, day names and AM/PM designators. The library looks up this culture by its id or tag. An application can give the culture itself, for example when it runs with invariant globalization, with a reduced ICU data set, or on netstandard1.0. The resolver gets the low 16 bits of the locale code, or for a language tag such as `[$-de-DE]` the id 0 and the tag, and the culture of the call. If it returns null, the library looks up the culture. To keep the names of the call for a code that it does not know, it returns the culture of the call:

```C#
var format = new NumberFormat("[$-407]d. mmmm yyyy");
Console.WriteLine(format.Format(44995, CultureInfo.InvariantCulture, false, (localeId, localeName, culture) => localeId == 0x407 ? myGermanCulture : culture));
```

`NumberFormat.DefaultLocaleCultureResolver` sets a resolver for all calls in the process, also in libraries that call `Format(value, culture)`:

```C#
NumberFormat.DefaultLocaleCultureResolver = (localeId, localeName, culture) => localeId == 0x407 ? myGermanCulture : culture;
```

## Features

- Parses and formats most custom number formats as expected: decimal, percent, thousands, exponential, fraction, currency, date/time, duration, text.
- Supports multiple sections with conditions.
- `GetColor(value)` gives the color of the section that formats the value, for example `[Red]` or `[Color10]`: a color name and a palette index from 1 to 56, or null.
- Formats values with relevant constants from CultureInfo.
- A locale code such as `[$-407]` selects the language of the month names, day names and AM/PM designators. A language tag, such as `[$-de-DE]`, has the same effect. Excel also writes a tag, such as `[$-en-US,1]`, in the formatCode16 attribute. numfmt also reads POSIX names, such as `[$-de_DE.utf8]` or `[$-de@euro]`, and the library reads them as tags. A code such as `[$-de]` is a locale id and a tag; the tag gives the culture if the id does not. Only `[$-411]` selects the Japanese era. `[$-x-sysdate]` and `[$-x-systime]` stay unknown codes. The separators always come from the CultureInfo. If the language of the locale code is not known, or for a locale id on netstandard1.0, the names also come from the CultureInfo. On netstandard1.0, an unknown tag can give the names of the invariant culture.
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
General  | General   |(any)|Excel General rules for numbers, else CompatibleConvert.ToString()
Text     | ;;;"Text: "@|string|Convert.ToString()

In case of errors, `Format()` returns the value from `CompatibleConvert.ToString()`.

`CompatibleConvert.ToString()` formats floats and doubles with explicit precision, or falls back to `Convert.ToString()` for any other types.
`ExcelDateTime.TryConvert()` uses DateTimes as is, or converts numeric values to a DateTime with adjustments for legacy Excel behaviors.

## TODO/notes

- 'General' ignores the column width. Numbers show at most 11 characters.
- No errors: Invalid format strings and incompatible input values are formatted with `.ToString()`.
- Variable width space is returned as regular space.
- Repeat-to-fill characters are printed once, not repeated.
- No alignment hinting.
- No date conditions.
