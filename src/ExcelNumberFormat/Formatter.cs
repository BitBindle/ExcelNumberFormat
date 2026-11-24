using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ExcelNumberFormat
{
    static internal class Formatter
    {
        private const int JapaneseLcid = 0x411;
        private const int MaxDigits = 15;

        static public string Format(object value, string formatString, CultureInfo culture, bool isDate1904)
        {
            var format = new NumberFormat(formatString);
            if (!format.IsValid)
                return CompatibleConvert.ToString(value, culture);

            var normalized = CompatibleConvert.Normalize(value);
            var section = Evaluator.GetSection(format.Sections, normalized);
            if (section == null)
                return CompatibleConvert.ToString(value, culture);

            return Format(normalized, section, culture, isDate1904);
        }

        static public string Format(object value, Section node, CultureInfo culture, bool isDate1904)
        {
            switch (node.Type)
            {
                case SectionType.Number:
                    return FormatNumber(ToDoubleHidingSign(value, node, culture), node.Number, culture);

                case SectionType.Date:
                    var dateDigits = GetFractionDigits(node.GeneralTextDateDurationParts);
                    var dateValue = dateDigits > 0 || ShowsTime(node) ? RoundSubSeconds(value, dateDigits) : value;
                    if (ExcelDateTime.TryConvert(dateValue, isDate1904, culture, out var excelDateTime))
                    {
                        return FormatDate(excelDateTime, node.GeneralTextDateDurationParts, culture, node.Lcid);
                    }
                    else
                    {
                        throw new FormatException("Unexpected date value");
                    }

                case SectionType.Duration:
                    var durationDigits = GetFractionDigits(node.GeneralTextDateDurationParts);
                    var durationValue = RoundSubSeconds(value, durationDigits);
                    if (durationValue is TimeSpan ts)
                    {
                        return FormatTimeSpan(ts, node.GeneralTextDateDurationParts, culture, node.Lcid);
                    }
                    else
                    {
                        var d = Convert.ToDouble(durationValue);
                        return FormatTimeSpan(RoundedDaysToTimeSpan(d, durationDigits), node.GeneralTextDateDurationParts, culture, node.Lcid);
                    }

                case SectionType.General:
                    return FormatGeneralText(FormatGeneral(value, node, culture), node.GeneralTextDateDurationParts);

                case SectionType.Text:
                    return FormatGeneralText(CompatibleConvert.ToString(value, culture), node.GeneralTextDateDurationParts);

                case SectionType.Exponential:
                    return FormatExponential(ToDoubleHidingSign(value, node, culture), node, culture);

                case SectionType.Fraction:
                    return FormatFraction(ToDoubleHidingSign(value, node, culture), node, culture);

                default:
                    throw new InvalidOperationException("Unknown number format section");
            }
        }

        /// <summary>
        /// True if the date section shows an hour, a minute or a second. A section that shows only the date,
        /// for example yyyy-mm-dd, dddd or the [$-F800] long date, does not round the time of day.
        /// </summary>
        static bool ShowsTime(Section node)
        {
            if (node.Lcid != null && node.Lcid.IsLongSystemDate)
                return false;

            var tokens = node.GeneralTextDateDurationParts;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.StartsWith("h", StringComparison.OrdinalIgnoreCase) || token.StartsWith("s", StringComparison.OrdinalIgnoreCase))
                    return true;

                // m is the minute after an hour or before a second, as in FormatDate
                if (token.StartsWith("m", StringComparison.OrdinalIgnoreCase) && (LookBackDatePart(tokens, i - 1, "h") || LookAheadDatePart(tokens, i + 1, "s")))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Number of fractional second digits requested by a ".0" token, or 0 if there is none. Excel supports up to 3.
        /// </summary>
        static int GetFractionDigits(List<string> tokens)
        {
            var digits = 0;
            foreach (var token in tokens)
            {
                if (token.StartsWith(".0"))
                    digits = Math.Min(token.Length - 1, 3);
            }

            return digits;
        }

        /// <summary>
        /// Prints the fractional seconds with exactly as many digits as the format asks for.
        /// The value is expected to be rounded already by RoundSubSeconds.
        /// The point before the digits is the decimal separator, as in a number format.
        /// </summary>
        static string FormatSubSeconds(int milliseconds, int digits, CultureInfo culture)
        {
            return culture.NumberFormat.NumberDecimalSeparator + milliseconds.ToString("D3", CultureInfo.InvariantCulture).Substring(0, Math.Min(digits, 3)).PadRight(digits, '0');
        }

        /// <summary>
        /// Rounds a date, time or duration to the displayed number of fractional second digits (half up, like Excel),
        /// so that a carry ripples into the seconds, minutes, hours and days.
        /// Without fractional second digits, the value rounds to whole seconds: 23:59:59.6 shows as 0:00:00 in h:mm:ss.
        /// Codes for larger units then show whole units only, so 9:00:36 shows as 9:00 in h:mm.
        /// A date section calls this with no digits only if it shows a time.
        /// </summary>
        static object RoundSubSeconds(object value, int digits)
        {
            if (value is double)
                return RoundSubSeconds((double)value, digits);

            if (value is DateTime)
            {
                var dt = (DateTime)value;
                var unit = TicksPerFractionDigit(digits);
                var roundedTicks = (dt.Ticks + unit / 2) / unit * unit;
                return roundedTicks > DateTime.MaxValue.Ticks ? dt : new DateTime(roundedTicks, dt.Kind);
            }

            if (value is TimeSpan)
            {
                var ts = (TimeSpan)value;
                var unit = TicksPerFractionDigit(digits);
                if (ts.Ticks == long.MinValue || Math.Abs(ts.Ticks) > long.MaxValue - unit)
                    return ts;

                var roundedTicks = (Math.Abs(ts.Ticks) + unit / 2) / unit * unit;
                return new TimeSpan(ts.Ticks < 0 ? -roundedTicks : roundedTicks);
            }

            return value;
        }

        /// <summary>
        /// Converts a day count that RoundSubSeconds rounded to a TimeSpan with the same number of 10^-digits seconds.
        /// TimeSpan.FromDays can cut the last tick, and then 1.005 days shows as 86831 seconds, not 86832.
        /// </summary>
        static TimeSpan RoundedDaysToTimeSpan(double days, int digits)
        {
            if (double.IsNaN(days) || double.IsInfinity(days) || Math.Abs(days) > 1e7)
                return TimeSpan.FromDays(days);

            var unit = TicksPerFractionDigit(digits);
            var units = Math.Round(days * (TimeSpan.TicksPerDay / unit));
            return new TimeSpan((long)units * unit);
        }

        static long TicksPerFractionDigit(int digits)
        {
            var unit = TimeSpan.TicksPerSecond;
            for (var i = 0; i < digits; i++)
                unit /= 10;
            return unit;
        }

        /// <summary>
        /// Rounds a numeric day count to a whole number of 10^-digits seconds, half away from zero on the exact value of the double.
        /// Excel does the same: 3661.25 seconds is stored just below .25 and shows as .2, while 12.35 is stored just above and shows as .4.
        /// Uses 128-bit integer arithmetic, as System.Numerics is not available on all target frameworks.
        /// </summary>
        static double RoundSubSeconds(double value, int digits)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > 1e7)
                return value;

            // |value| = mantissa * 2^exponent
            var bits = BitConverter.DoubleToInt64Bits(Math.Abs(value));
            var exponent = (int)((bits >> 52) & 0x7FF);
            var mantissa = (ulong)(bits & 0xFFFFFFFFFFFFFL);
            if (exponent == 0)
                exponent++;
            else
                mantissa |= 1UL << 52;
            exponent -= 1075;

            // Units per day: 86400 seconds, each split in 10^digits parts
            ulong unitsPerDay = 86400;
            for (var i = 0; i < digits; i++)
                unitsPerDay *= 10;

            ulong units;
            if (exponent >= 0)
            {
                units = (mantissa << exponent) * unitsPerDay;
            }
            else
            {
                // mantissa * unitsPerDay needs up to 80 bits: (hi, lo) = p1 * 2^32 + p0
                var p0 = (mantissa & 0xFFFFFFFFUL) * unitsPerDay;
                var p1 = (mantissa >> 32) * unitsPerDay;
                var lo = p0 + (p1 << 32);
                var hi = (p1 >> 32) + (lo < p0 ? 1UL : 0UL);

                // Add one half, then divide by 2^shift
                var shift = -exponent;
                if (shift > 80)
                {
                    units = 0;
                }
                else
                {
                    if (shift - 1 < 64)
                    {
                        var half = 1UL << (shift - 1);
                        lo += half;
                        if (lo < half)
                            hi++;
                    }
                    else
                    {
                        hi += 1UL << (shift - 65);
                    }

                    units = shift < 64 ? (lo >> shift) | (hi << (64 - shift)) : hi >> (shift - 64);
                }
            }

            var rounded = (double)units / (double)unitsPerDay;
            return value < 0 ? -rounded : rounded;
        }

        /// <summary>
        /// Formats a number with the General rules of Excel, and other values as text.
        /// A General section hides the sign of a number under the same conditions as the other number sections.
        /// </summary>
        static string FormatGeneral(object value, Section node, CultureInfo culture)
        {
            if (value is double || value is int || value is short)
                return FormatGeneral(ToDoubleHidingSign(value, node, culture), culture);

            return CompatibleConvert.ToString(value, culture);
        }

        /// <summary>
        /// Formats a number like the General format of Excel. The text has at most 11 characters, not counting the minus sign (ECMA-376 Part 1, 18.8.30).
        /// The decimal point and the leading zero of a number below 1 count. The rounding is half away from zero on the 15 significant digits that Excel keeps.
        /// Excel uses the scientific form 0.#####E+00 for 12 or more integer digits.
        /// It also uses it when the scientific form shows more significant digits than the decimal form,
        /// or when the decimal form rounds up to a power of ten that the scientific form does not reach (5E-10 is not 0.000000001).
        /// </summary>
        static string FormatGeneral(double value, CultureInfo culture)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return CompatibleConvert.ToString(value, culture);

            // Also negative zero
            if (value == 0)
                return "0";

            var numberFormat = culture.NumberFormat;
            var sign = value < 0 ? numberFormat.NegativeSign : string.Empty;
            var absolute = Math.Abs(value);

            // G15 gives the 15 significant digits that Excel keeps. If they fit in 11 characters, the decimal form shows them all.
            var text = absolute.ToString("G15", CultureInfo.InvariantCulture);
            var exponentIndex = text.IndexOf('E');
            if (text.Length <= 11 && exponentIndex < 0)
            {
                if (numberFormat.NumberDecimalSeparator != ".")
                    text = text.Replace(".", numberFormat.NumberDecimalSeparator);
                return sign + text;
            }

            // The significant digits and the exponent of "d.dddE+dd", "ddd.ddd" or "0.000ddd".
            // The digits use the first 15 characters, and the rounded digits of the decimal form the next 15 characters.
            var digits = new char[2 * MaxDigits];
            var digitCount = 0;
            int exponent;
            if (exponentIndex >= 0)
            {
                for (var i = 0; i < exponentIndex; i++)
                {
                    if (text[i] != '.')
                        digits[digitCount++] = text[i];
                }

                exponent = 0;
                for (var i = exponentIndex + 2; i < text.Length; i++)
                    exponent = exponent * 10 + (text[i] - '0');
                if (text[exponentIndex + 1] == '-')
                    exponent = -exponent;
            }
            else
            {
                var point = text.IndexOf('.');
                exponent = (point < 0 ? text.Length : point) - 1;
                foreach (var c in text)
                {
                    if (c == '.')
                        continue;

                    if (digitCount == 0 && c == '0')
                        exponent--;
                    else
                        digits[digitCount++] = c;
                }
            }

            // The decimal form uses the characters that the integer digits and the decimal point do not use, or 9 decimals after "0."
            var roundedIndex = MaxDigits;
            var roundedExponent = exponent;
            var decimals = exponent >= 0 ? Math.Max(0, 9 - exponent) : 9;
            var roundedCount = RoundSignificantDigits(digits, digitCount, exponent + 1 + decimals, roundedIndex, ref roundedExponent);

            // From 1E-4 up, the decimal form shows 6 or more significant digits. Then only 12 or more integer digits need the scientific form.
            var scientific = roundedExponent >= 11;
            if (scientific || exponent < -4)
            {
                var scientificExponent = exponent;
                var scientificCount = RoundSignificantDigits(digits, digitCount, 6, 0, ref scientificExponent);
                if (scientific || roundedCount < scientificCount || roundedExponent != scientificExponent)
                {
                    scientific = true;
                    roundedIndex = 0;
                    roundedCount = scientificCount;
                    roundedExponent = scientificExponent;
                }
            }

            var decimalSeparator = numberFormat.NumberDecimalSeparator;
            var exponentSign = roundedExponent < 0 ? numberFormat.NegativeSign : numberFormat.PositiveSign;
            var result = new char[sign.Length + decimalSeparator.Length + exponentSign.Length + 2 * MaxDigits];
            var length = Append(result, 0, sign);
            if (scientific)
            {
                result[length++] = digits[0];
                if (roundedCount > 1)
                {
                    length = Append(result, length, decimalSeparator);
                    length = Append(result, length, digits, 1, roundedCount - 1);
                }

                result[length++] = 'E';
                length = Append(result, length, exponentSign);
                var absoluteExponent = Math.Abs(roundedExponent);
                if (absoluteExponent >= 100)
                    result[length++] = (char)('0' + absoluteExponent / 100);
                result[length++] = (char)('0' + absoluteExponent / 10 % 10);
                result[length++] = (char)('0' + absoluteExponent % 10);
            }
            else if (roundedExponent >= 0)
            {
                var integerLength = roundedExponent + 1;
                length = Append(result, length, digits, roundedIndex, Math.Min(roundedCount, integerLength));
                for (var i = roundedCount; i < integerLength; i++)
                    result[length++] = '0';

                if (roundedCount > integerLength)
                {
                    length = Append(result, length, decimalSeparator);
                    length = Append(result, length, digits, roundedIndex + integerLength, roundedCount - integerLength);
                }
            }
            else
            {
                result[length++] = '0';
                length = Append(result, length, decimalSeparator);
                for (var i = 0; i < -roundedExponent - 1; i++)
                    result[length++] = '0';
                length = Append(result, length, digits, roundedIndex, roundedCount);
            }

            return new string(result, 0, length);
        }

        static int Append(char[] buffer, int index, string text)
        {
            text.CopyTo(0, buffer, index, text.Length);
            return index + text.Length;
        }

        static int Append(char[] buffer, int index, char[] source, int sourceIndex, int count)
        {
            Array.Copy(source, sourceIndex, buffer, index, count);
            return index + count;
        }

        /// <summary>
        /// Rounds the first digitCount digits of the array to count significant digits, half away from zero, and removes the trailing zeros.
        /// Writes the result to the array from resultIndex, and returns the number of digits, or 0 if the value rounds to zero.
        /// A carry to the next power of ten increments the exponent.
        /// </summary>
        static int RoundSignificantDigits(char[] digits, int digitCount, int count, int resultIndex, ref int exponent)
        {
            if (count < 0)
                return 0;

            if (count > digitCount)
                count = digitCount;

            var roundUp = count < digitCount && digits[count] >= '5';
            Array.Copy(digits, 0, digits, resultIndex, count);
            if (roundUp)
            {
                var i = count - 1;
                while (i >= 0 && digits[resultIndex + i] == '9')
                {
                    digits[resultIndex + i] = '0';
                    i--;
                }

                if (i < 0)
                {
                    exponent++;
                    digits[resultIndex] = '1';
                    return 1;
                }

                digits[resultIndex + i]++;
            }

            while (count > 0 && digits[resultIndex + count - 1] == '0')
                count--;

            return count;
        }

        static string FormatGeneralText(string text, List<string> tokens)
        {
            var result = new StringBuilder();
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (Token.IsGeneral(token) || token == "@")
                {
                    result.Append(text);
                }
                else
                {
                    FormatLiteral(token, result);
                }
            }
            return result.ToString();
        }

        private static string FormatTimeSpan(TimeSpan timeSpan, List<string> tokens, CultureInfo culture, WindowsLanguageCodeIdentifier lcid)
        {
            if (lcid != null && lcid.IsLongSystemTime && lcid.TimeTokens?.Count > 0)
            {
                tokens = lcid.TimeTokens;
            }

            // NOTE/TODO: assumes there is exactly one [hh], [mm] or [ss] using the integer part of TimeSpan.TotalXXX when formatting.
            // The timeSpan input is then truncated to the remainder fraction, which is used to format mm and/or ss.
            var result = new StringBuilder();
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (token.StartsWith("m", StringComparison.OrdinalIgnoreCase))
                {
                    var value = timeSpan.Minutes;
                    var digits = token.Length;
                    result.Append(value.ToString("D" + digits));
                }
                else if (token.StartsWith("s", StringComparison.OrdinalIgnoreCase))
                {
                    // RoundSubSeconds already rounded the value to the displayed digits
                    var value = timeSpan.Seconds;
                    var digits = token.Length;
                    result.Append(value.ToString("D" + digits));
                }
                else if (token.StartsWith("[h", StringComparison.OrdinalIgnoreCase))
                {
                    var value = (long)timeSpan.TotalHours;
                    var digits = token.Length - 2;
                    result.Append(value.ToString("D" + digits));
                    timeSpan = new TimeSpan(0, 0, Math.Abs(timeSpan.Minutes), Math.Abs(timeSpan.Seconds), Math.Abs(timeSpan.Milliseconds));
                }
                else if (token.StartsWith("[m", StringComparison.OrdinalIgnoreCase))
                {
                    var value = (long)timeSpan.TotalMinutes;
                    var digits = token.Length - 2;
                    result.Append(value.ToString("D" + digits));
                    timeSpan = new TimeSpan(0, 0, 0, Math.Abs(timeSpan.Seconds), Math.Abs(timeSpan.Milliseconds));
                }
                else if (token.StartsWith("[s", StringComparison.OrdinalIgnoreCase))
                {
                    var value = (long)timeSpan.TotalSeconds;
                    var digits = token.Length - 2;
                    result.Append(value.ToString("D" + digits));
                    timeSpan = new TimeSpan(0, 0, 0, 0, Math.Abs(timeSpan.Milliseconds));
                }
                else if (token.StartsWith(".0")) {
                    result.Append(FormatSubSeconds(timeSpan.Milliseconds, token.Length - 1, culture));
                }
                else
                {
                    FormatLiteral(token, result);
                }
            }

            return result.ToString();
        }

        private static string FormatDate(ExcelDateTime date, List<string> tokens, CultureInfo culture, WindowsLanguageCodeIdentifier lcid)
        {
            if (lcid != null && lcid.IsLongSystemDate)
            {
                return date.ToString(culture.DateTimeFormat.LongDatePattern, culture);
            }

            var containsAmPm = ContainsAmPm(tokens);

            // Only the Japanese locale prints the era name and counts years from the start of the era
            var japaneseEra = lcid != null && lcid.LocaleId == JapaneseLcid ? JapaneseEra.Find(date.AdjustedDateTime) : null;

            // The locale code selects the language of the month names, day names and AM/PM designators.
            // The separators, and the names for an unknown locale code, come from the culture of the call.
            var names = lcid?.Culture ?? culture;

            var result = new StringBuilder();
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (token.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                {
                    // year
                    var digits = token.Length;
                    if (digits < 2)
                        digits = 2;
                    if (digits == 3)
                        digits = 4;

                    var year = date.Year;
                    if (digits == 2)
                        year = year % 100;

                    result.Append(year.ToString("D" + digits));
                }
                else if (token.StartsWith("m", StringComparison.OrdinalIgnoreCase))
                {
                    // If  "m" or "mm" code is used immediately after the "h" or "hh" code (for hours) or immediately before 
                    // the "ss" code (for seconds), the application shall display minutes instead of the month. 
                    if (LookBackDatePart(tokens, i - 1, "h") || LookAheadDatePart(tokens, i + 1, "s"))
                    {
                        var digits = token.Length;
                        result.Append(date.Minute.ToString("D" + digits));
                    }
                    else
                    {
                        var digits = token.Length;
                        if (digits == 3)
                        {
                            result.Append(names.DateTimeFormat.AbbreviatedMonthNames[date.Month - 1]);
                        }
                        else if (digits == 4)
                        {
                            result.Append(names.DateTimeFormat.MonthNames[date.Month - 1]);
                        }
                        else if (digits == 5)
                        {
                            result.Append(names.DateTimeFormat.MonthNames[date.Month - 1][0]);
                        }
                        else
                        {
                            result.Append(date.Month.ToString("D" + digits));
                        }
                    }
                }
                else if (token.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                {
                    var digits = token.Length;
                    if (digits == 3)
                    {
                        // Sun-Sat
                        result.Append(names.DateTimeFormat.AbbreviatedDayNames[(int)date.DayOfWeek]);
                    }
                    else if (digits == 4)
                    {
                        // Sunday-Saturday
                        result.Append(names.DateTimeFormat.DayNames[(int)date.DayOfWeek]);
                    }
                    else
                    {
                        result.Append(date.Day.ToString("D" + digits));
                    }
                }
                else if (token.StartsWith("h", StringComparison.OrdinalIgnoreCase))
                {
                    var digits = token.Length;
                    if (containsAmPm)
                        result.Append(((date.Hour + 11) % 12 + 1).ToString("D" + digits));
                    else
                        result.Append(date.Hour.ToString("D" + digits));
                }
                else if (token.StartsWith("s", StringComparison.OrdinalIgnoreCase))
                {
                    var digits = token.Length;
                    result.Append(date.Second.ToString("D" + digits));
                }
                else if (token.StartsWith("g", StringComparison.OrdinalIgnoreCase) && japaneseEra != null)
                {
                    result.Append(token.Length == 1 ? japaneseEra.Initial : token.Length == 2 ? japaneseEra.ShortName : japaneseEra.Name);
                }
                else if (token[0] == 'e')
                {
                    // Other locales print the four digit year
                    if (japaneseEra != null)
                        result.Append((date.Year - japaneseEra.StartYear + 1).ToString("D" + (Token.IsExponent(token) ? 1 : Math.Min(token.Length, 2))));
                    else
                        result.Append(date.Year.ToString("D4"));

                    // "e-" and "e+" are exponents in a number, but an era year and a literal in a date
                    if (Token.IsExponent(token))
                        result.Append(token[1]);
                }
                else if (token.StartsWith("g", StringComparison.OrdinalIgnoreCase))
                {
                    var era = culture.DateTimeFormat.Calendar.GetEra(date.AdjustedDateTime);
                    var digits = token.Length;
                    if (digits < 3)
                    {
                        result.Append(culture.DateTimeFormat.GetAbbreviatedEraName(era));
                    }
                    else
                    {
                        result.Append(culture.DateTimeFormat.GetEraName(era));
                    }
                }
                else if (string.Compare(token, "am/pm", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    result.Append(GetAmPmDesignator(date.Hour, names));
                }
                else if (string.Compare(token, "a/p", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Excel only keeps the letters when the format has no locale code
                    var ampm = date.ToString("%t", CultureInfo.InvariantCulture);
                    if (lcid != null && lcid.LocaleId != 0)
                    {
                        result.Append(GetAmPmDesignator(date.Hour, names));
                    }
                    else if (char.IsUpper(token[0]))
                    {
                        result.Append(ampm.ToUpperInvariant());
                    }
                    else
                    {
                        result.Append(ampm.ToLowerInvariant());
                    }
                }
                else if (token.StartsWith(".0"))
                {
                    result.Append(FormatSubSeconds(date.Millisecond, token.Length - 1, culture));
                }
                else if (token == "/")
                {
#if NETSTANDARD1_0
                    result.Append(DateTime.MaxValue.ToString("/d", culture)[0]);
#else
                    result.Append(culture.DateTimeFormat.DateSeparator);
#endif
                }
                else if (token == ",")
                {
                    while (i < tokens.Count - 1 && tokens[i + 1] == ",")
                    {
                        i++;
                    }

                    result.Append(",");
                }
                else
                {
                    FormatLiteral(token, result);
                }
            }

            return result.ToString();
        }

        private static string GetAmPmDesignator(int hour, CultureInfo format)
        {
            var designator = hour < 12 ? format.DateTimeFormat.AMDesignator : format.DateTimeFormat.PMDesignator;
            return string.IsNullOrEmpty(designator) ? (hour < 12 ? "AM" : "PM") : designator;
        }

        private static bool LookAheadDatePart(List<string> tokens, int fromIndex, string startsWith)
        {
            for (var i = fromIndex; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (Token.IsDatePart(token))
                    return false;
            }

            return false;
        }

        private static bool LookBackDatePart(List<string> tokens, int fromIndex, string startsWith)
        {
            for (var i = fromIndex; i >= 0; i--)
            {
                var token = tokens[i];
                if (token.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (Token.IsDatePart(token))
                    return false;
            }

            return false;
        }

        private static bool ContainsAmPm(List<string> tokens)
        {
            foreach (var token in tokens)
            {
                if (string.Compare(token, "am/pm", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }

                if (string.Compare(token, "a/p", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        // Hide sign under certain conditions and section index
        static double ToDoubleHidingSign(object value, Section node, CultureInfo culture)
        {
            var number = Convert.ToDouble(value, culture);
            if ((node.SectionIndex == 0 && node.Condition != null) || node.SectionIndex == 1)
                number = Math.Abs(number);

            return number;
        }

        static string FormatNumber(double value, DecimalSection format, CultureInfo culture)
        {
            bool thousandSeparator = format.ThousandSeparator;
            value = value / format.ThousandDivisor;
            if (value != 0)
                value = value * format.PercentMultiplier;

            var result = new StringBuilder();
            FormatNumber(value, format.BeforeDecimal, format.DecimalSeparator, format.AfterDecimal, thousandSeparator, culture, result);
            return result.ToString();
        }

        static void FormatNumber(double value, List<string> beforeDecimal, bool decimalSeparator, List<string> afterDecimal, bool thousandSeparator, CultureInfo culture, StringBuilder result)
        {
            int signitificantDigits = 0;
            if (afterDecimal != null)
                signitificantDigits = GetDigitCount(afterDecimal);

            var valueString = RoundToFixed(Math.Abs(value), signitificantDigits);
            var valueStrings = valueString.Split('.');
            var thousandsString = valueStrings[0];
            var decimalString = valueStrings.Length > 1 ? valueStrings[1].TrimEnd('0') : "";

            if (value < 0)
            {
                result.Append("-");
            }

            if (beforeDecimal != null)
            {
                FormatThousands(thousandsString, thousandSeparator, false, beforeDecimal, culture, result);
            }

            if (decimalSeparator) { 
                result.Append(culture.NumberFormat.NumberDecimalSeparator);
            }

            if (afterDecimal != null)
            {
                FormatDecimals(decimalString, afterDecimal, result);
            }
        }

        /// <summary>
        /// Formats a non-negative value with a fixed number of decimals, rounding half away from zero like Excel.
        /// The double is first reduced to 15 significant digits, as Excel does, so 2.675 rounds to 2.68.
        /// </summary>
        static string RoundToFixed(double value, int digits)
        {
            if (digits <= 28 && value < 7.9e28)
            {
                var decimalValue = decimal.Parse(value.ToString("G15", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
                var rounded = Math.Round(decimalValue, digits, MidpointRounding.AwayFromZero);
                return rounded.ToString("F" + digits, CultureInfo.InvariantCulture);
            }

            return value.ToString("F" + digits, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Prints right-aligned, left-padded integer before the decimal separator. With optional most-significant zero.
        /// </summary>
        public static void FormatThousands(string valueString, bool thousandSeparator, bool significantZero, List<string> tokens, CultureInfo culture, StringBuilder result)
        {
            var significant = false;
            var formatDigits = GetDigitCount(tokens);
            valueString = valueString.PadLeft(formatDigits, '0');

            // Print literals occurring before any placeholders
            var tokenIndex = 0;
            for (; tokenIndex < tokens.Count; tokenIndex++)
            {
                var token = tokens[tokenIndex];
                if (Token.IsPlaceholder(token))
                    break;
                else
                    FormatLiteral(token, result);
            }

            // Print value digits until there are as many digits remaining as there are placeholders
            var digitIndex = 0;
            for (; digitIndex < (valueString.Length - formatDigits); digitIndex++)
            {
                significant = true;
                result.Append(valueString[digitIndex]);

                if (thousandSeparator)
                    FormatThousandSeparator(valueString, digitIndex, culture, result);
            }

            // Print remaining value digits and format literals
            for (; tokenIndex < tokens.Count; ++tokenIndex)
            {
                var token = tokens[tokenIndex];
                if (Token.IsPlaceholder(token))
                {
                    var c = valueString[digitIndex];
                    if (c != '0' || (significantZero && digitIndex == valueString.Length - 1)) significant = true;

                    FormatPlaceholder(token, c, significant, result);

                    if (thousandSeparator && (significant || token.Equals("0")))
                        FormatThousandSeparator(valueString, digitIndex, culture, result);

                    digitIndex++;
                }
                else
                {
                    FormatLiteral(token, result);
                }
            }
        }

        static void FormatThousandSeparator(string valueString, int digit, CultureInfo culture, StringBuilder result)
        {
            var positionInTens = valueString.Length - 1 - digit;
            if (positionInTens > 0 && (positionInTens % 3) == 0)
            {
                result.Append(culture.NumberFormat.NumberGroupSeparator);
            }
        }

        /// <summary>
        /// Prints left-aligned, right-padded integer after the decimal separator. Does not print significant zero.
        /// </summary>
        public static void FormatDecimals(string valueString, List<string> tokens, StringBuilder result)
        {
            var significant = true;
            var unpaddedDigits = valueString.Length;
            var formatDigits = GetDigitCount(tokens);

            valueString = valueString.PadRight(formatDigits, '0');

            // Print all format digits
            var valueIndex = 0;
            for (var tokenIndex = 0; tokenIndex < tokens.Count; ++tokenIndex)
            {
                var token = tokens[tokenIndex];
                if (Token.IsPlaceholder(token))
                {
                    var c = valueString[valueIndex];
                    significant = valueIndex < unpaddedDigits;

                    FormatPlaceholder(token, c, significant, result);
                    valueIndex++;
                }
                else
                {
                    FormatLiteral(token, result);
                }
            }
        }

        static string FormatExponential(double value, Section format, CultureInfo culture)
        {
            // NaN and infinity have no digits to lay out
            if (double.IsNaN(value) || double.IsInfinity(value))
                return value.ToString(culture);

            // The application shall display a number to the right of 
            // the "E" symbol that corresponds to the number of places that 
            // the decimal point was moved. 

            var baseDigits = 0;
            if (format.Exponential.BeforeDecimal != null)
            {
                baseDigits = GetDigitCount(format.Exponential.BeforeDecimal);
            }

            var exponent = 0;
            var mantissa = value;
            var step = Math.Max(baseDigits, 1);
            var afterDecimalDigits = format.Exponential.AfterDecimal != null ? GetDigitCount(format.Exponential.AfterDecimal) : 0;

            // Zero has no exponent
            if (value != 0)
            {
                exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
                mantissa = value / Math.Pow(10, exponent);

                var shift = baseDigits > 0 ? Math.Abs(exponent) % baseDigits : 0;
                if (shift > 0)
                {
                    if (exponent < 0)
                        shift = (baseDigits - shift);

                    mantissa *= Math.Pow(10, shift);
                    exponent -= shift;
                }

                // Rounding the mantissa can carry it to the next power of ten, 9.999 with 0.00E+00 is 1.00E+01
                var rounded = double.Parse(Math.Abs(mantissa).ToString("F" + afterDecimalDigits, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                if (rounded >= Math.Pow(10, step))
                {
                    mantissa /= Math.Pow(10, step);
                    exponent += step;
                }
            }

            var result = new StringBuilder();
            FormatNumber(mantissa, format.Exponential.BeforeDecimal, format.Exponential.DecimalSeparator, format.Exponential.AfterDecimal, false, culture, result);

            result.Append(format.Exponential.ExponentialToken[0]);

            if (format.Exponential.ExponentialToken[1] == '+' && exponent >= 0)
            {
                result.Append("+");
            }
            else if (exponent < 0)
            {
                result.Append("-");
            }

            FormatThousands(Math.Abs(exponent).ToString(CultureInfo.InvariantCulture), false, false, format.Exponential.Power, culture, result);
            return result.ToString();
        }

        static string FormatFraction(double value, Section format, CultureInfo culture)
        {
            // NaN and infinity have no digits to lay out
            if (double.IsNaN(value) || double.IsInfinity(value))
                return value.ToString(culture);

            // A double, the whole part can be larger than an int
            double integral = 0;
            int numerator, denominator;

            bool sign = value < 0;
            value = Math.Abs(value);

            if (format.Fraction.IntegerPart != null)
            {
                integral = Math.Truncate(value);
                value -= integral;
            }

            if (format.Fraction.DenominatorConstant != 0)
            {
                denominator = format.Fraction.DenominatorConstant;
                var rr = Math.Round(value * denominator);

                // Without an integer part the fraction stays improper (7/4), otherwise a carry goes to the integer part
                if (format.Fraction.IntegerPart != null)
                {
                    var carry = Math.Floor(rr / denominator);
                    integral += carry;
                    rr -= carry * denominator;
                }

                numerator = (int)rr;
            }
            else
            {
                var denominatorDigits = Math.Min(GetDigitCount(format.Fraction.Denominator), 7);
                GetFraction(value, (int)Math.Pow(10, denominatorDigits) - 1, out numerator, out denominator);

                if (format.Fraction.IntegerPart != null && numerator >= denominator)
                {
                    integral += numerator / denominator;
                    numerator = 0;
                    denominator = 1;
                }
            }

            // Don't hide fraction if at least one zero in the numerator format
            var numeratorZeros = GetZeroCount(format.Fraction.Numerator);
            var hideFraction = (format.Fraction.IntegerPart != null && numerator == 0 && numeratorZeros == 0);

            var result = new StringBuilder();

            if (sign)
                result.Append("-");

            // Print integer part with significant zero if fraction part is hidden
            if (format.Fraction.IntegerPart != null)
                FormatThousands(integral.ToString("F0", CultureInfo.InvariantCulture), false, hideFraction, format.Fraction.IntegerPart, culture, result);

            var numeratorString = Math.Abs(numerator).ToString("F0", CultureInfo.InvariantCulture);
            var denominatorString = denominator.ToString("F0", CultureInfo.InvariantCulture);

            var fraction = new StringBuilder();

            FormatThousands(numeratorString, false, true, format.Fraction.Numerator, culture, fraction);

            fraction.Append("/");

            if (format.Fraction.DenominatorPrefix != null)
                FormatThousands("", false, false, format.Fraction.DenominatorPrefix, culture, fraction);

            if (format.Fraction.DenominatorConstant != 0)
                fraction.Append(format.Fraction.DenominatorConstant.ToString());
            else
                FormatDenominator(denominatorString, format.Fraction.Denominator, fraction);

            if (format.Fraction.DenominatorSuffix != null)
                FormatThousands("", false, false, format.Fraction.DenominatorSuffix, culture, fraction);

            if (hideFraction)
                result.Append(new string(' ', fraction.ToString().Length));
            else
                result.Append(fraction.ToString());

            if (format.Fraction.FractionSuffix != null)
                FormatThousands("", false, false, format.Fraction.FractionSuffix, culture, result);

            return result.ToString();
        }

        // Adapted from ssf.js 'frac()' helper
        static void GetFraction(double x, int D, out int nom, out int den)
        {
            var sgn = x < 0 ? -1 : 1;
            var B = x * sgn;
            var P_2 = 0.0;
            var P_1 = 1.0;
            var P = 0.0;
            var Q_2 = 1.0;
            var Q_1 = 0.0;
            var Q = 0.0;
            var A = Math.Floor(B);
            while (Q_1 < D)
            {
                A = Math.Floor(B);
                P = A * P_1 + P_2;
                Q = A * Q_1 + Q_2;
                if ((B - A) < 0.00000005) break;
                B = 1 / (B - A);
                P_2 = P_1; P_1 = P;
                Q_2 = Q_1; Q_1 = Q;
            }
            if (Q > D) { if (Q_1 > D) { Q = Q_2; P = P_2; } else { Q = Q_1; P = P_1; } }
            nom = (int)(sgn * P);
            den = (int)Q;
        }

        /// <summary>
        /// Prints left-aligned, left-padded fraction integer denominator.
        /// Assumes tokens contain only placeholders, valueString has fewer or equal number of digits as tokens.
        /// </summary>
        public static void FormatDenominator(string valueString, List<string> tokens, StringBuilder result)
        {
            var formatDigits = GetDigitCount(tokens);
            valueString = valueString.PadLeft(formatDigits, '0');

            bool significant = false;
            var valueIndex = 0;
            for (var tokenIndex = 0; tokenIndex < tokens.Count; ++tokenIndex)
            {
                var token = tokens[tokenIndex];
                char c;
                if (valueIndex < valueString.Length) {
                    c = GetLeftAlignedValueDigit(token, valueString, valueIndex, significant, out valueIndex);

                    if (c != '0')
                        significant = true;
                } else { 
                    c = '0';
                    significant = false;
                }

                FormatPlaceholder(token, c, significant, result);
            }
        }

        /// <summary>
        /// Returns the first digit from valueString. If the token is '?' 
        /// returns the first significant digit from valueString, or '0' if there are no significant digits.
        /// The out valueIndex parameter contains the offset to the next digit in valueString.
        /// </summary>
        static char GetLeftAlignedValueDigit(string token, string valueString, int startIndex, bool significant, out int valueIndex)
        {
            char c;
            valueIndex = startIndex;
            if (valueIndex < valueString.Length)
            {
                c = valueString[valueIndex];
                valueIndex++;

                if (c != '0')
                    significant = true;

                if (token == "?" && !significant)
                {
                    // Eat insignificant zeros to left align denominator
                    while (valueIndex < valueString.Length)
                    {
                        c = valueString[valueIndex];
                        valueIndex++;

                        if (c != '0')
                        {
                            significant = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                c = '0';
                significant = false;
            }

            return c;
        }

        static void FormatPlaceholder(string token, char c, bool significant, StringBuilder result)
        {
            if (token == "0")
            {
                if (significant)
                    result.Append(c);
                else
                    result.Append("0");
            }
            else if (token == "#")
            {
                if (significant)
                    result.Append(c);
            }
            else if (token == "?")
            {
                if (significant)
                    result.Append(c);
                else
                    result.Append(" ");
            }
        }

        static int GetDigitCount(List<string> tokens)
        {
            var counter = 0;
            foreach (var token in tokens)
            {
                if (Token.IsPlaceholder(token))
                {
                    counter++;
                }
            }
            return counter;
        }

        static int GetZeroCount(List<string> tokens)
        {
            var counter = 0;
            foreach (var token in tokens)
            {
                if (token == "0")
                {
                    counter++;
                }
            }
            return counter;
        }

        static void FormatLiteral(string token, StringBuilder result)
        {
            string literal = string.Empty;
            if (token == ",")
            {
                ; // skip commas
            }
            else if (token.Length == 2 && (token[0] == '*' || token[0] == '\\'))
            {
                // TODO: * = repeat to fill cell
                literal = token[1].ToString();
            }
            else if (token.Length == 2 && token[0] == '_')
            {
                literal = " ";
            }
            else if (token.StartsWith("\""))
            {
                literal = token.Substring(1, token.Length - 2);
            }
            else
            {
                literal = token;
            }
            result.Append(literal);
        }
    }
}
