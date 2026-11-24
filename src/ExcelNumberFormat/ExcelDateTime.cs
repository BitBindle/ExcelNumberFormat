using System;
using System.Globalization;
using System.Text;

namespace ExcelNumberFormat
{
    /// <summary>
    /// Similar to regular .NET DateTime, but also supports 0/1 1900 and 29/2 1900.
    /// </summary>
    internal class ExcelDateTime
    {
        /// <summary>
        /// The closest .NET DateTime to the specified excel date. 
        /// </summary>
        public DateTime AdjustedDateTime { get; }

        /// <summary>
        /// Number of days to adjust by in post.
        /// </summary>
        public int AdjustDaysPost { get; }

        /// <summary>
        /// Constructs a new ExcelDateTime from a numeric value.
        /// </summary>
        public ExcelDateTime(double numericDate, bool isDate1904)
        {
            if (isDate1904)
            {
                numericDate += 1462.0;
                AdjustedDateTime = new DateTime(DoubleDateToTicks(numericDate), DateTimeKind.Unspecified);
                DayOfWeek = AdjustedDateTime.DayOfWeek;
            }
            else
            {
                // internal dates before 30/12/1899 should add two days to get the real date
                // internal dates on 30/12 19899 should add two days, but subtract a day post to get the real date
                // internal dates before 28/2/1900 should add one day to get the real date
                // internal dates on 28/2 1900 should use the same date, but add a day post to get the real date

                var internalDateTime = new DateTime(DoubleDateToTicks(numericDate), DateTimeKind.Unspecified);
                if (internalDateTime < Excel1900ZeroethMinDate)
                {
                    AdjustDaysPost = 0;
                    AdjustedDateTime = internalDateTime.AddDays(2);
                }

                else if (internalDateTime < Excel1900ZeroethMaxDate)
                {
                    AdjustDaysPost = -1;
                    AdjustedDateTime = internalDateTime.AddDays(2);
                }

                else if (internalDateTime < Excel1900LeapMinDate)
                {
                    AdjustDaysPost = 0;
                    AdjustedDateTime = internalDateTime.AddDays(1);
                }

                else if (internalDateTime < Excel1900LeapMaxDate)
                {
                    AdjustDaysPost = 1;
                    AdjustedDateTime = internalDateTime;
                }
                else
                {
                    AdjustDaysPost = 0;
                    AdjustedDateTime = internalDateTime;
                }

                // Excel counts 29/2 1900, so before 1/3 1900 its weekday is one day before the weekday of the real date.
                // The internal date has the weekday of Excel: 0 is a Saturday and 1 is a Sunday.
                // Internal dates before 30/12/1899 are not Excel dates, and keep the weekday of the real date.
                DayOfWeek = internalDateTime < Excel1900ZeroethMinDate ? AdjustedDateTime.DayOfWeek : internalDateTime.DayOfWeek;
            }
        }

        static DateTime Excel1900LeapMinDate = new DateTime(1900, 2, 28);
        static DateTime Excel1900LeapMaxDate = new DateTime(1900, 3, 1);
        static DateTime Excel1900ZeroethMinDate = new DateTime(1899, 12, 30);
        static DateTime Excel1900ZeroethMaxDate = new DateTime(1899, 12, 31);

        /// <summary>
        /// Wraps a regular .NET datetime.
        /// </summary>
        /// <param name="value"></param>
        public ExcelDateTime(DateTime value)
        {
            AdjustedDateTime = value;
            AdjustDaysPost = 0;
            DayOfWeek = value.DayOfWeek;
        }

        public int Year => AdjustedDateTime.Year;

        public int Month => AdjustedDateTime.Month;

        public int Day => AdjustedDateTime.Day + AdjustDaysPost;

        public int Hour => AdjustedDateTime.Hour;

        public int Minute => AdjustedDateTime.Minute;

        public int Second => AdjustedDateTime.Second;

        public int Millisecond => AdjustedDateTime.Millisecond;

        /// <summary>
        /// The weekday in the calendar of the date system. In the 1900 date system, 0 is a Saturday and 1 is a Sunday.
        /// </summary>
        public DayOfWeek DayOfWeek { get; }

        /// <summary>
        /// Formats the date with a .NET date pattern, for example the long date pattern of the culture.
        /// </summary>
        public string ToString(string numberFormat, CultureInfo culture)
        {
            // From 0/1 1900 to 29/2 1900 the day or the weekday is not the one of the adjusted date.
            // Then the day, month, year and weekday fields of the pattern become literals of the Excel date.
            // A culture with another calendar shows the adjusted date.
            if ((AdjustDaysPost == 0 && DayOfWeek == AdjustedDateTime.DayOfWeek) || !IsGregorian(culture.DateTimeFormat.Calendar))
                return AdjustedDateTime.ToString(numberFormat, culture);

            return AdjustedDateTime.ToString(ReplaceDateFields(numberFormat, culture.DateTimeFormat), culture);
        }

        private static bool IsGregorian(Calendar calendar)
        {
#if NETSTANDARD1_0
            return calendar.GetType().Name == "GregorianCalendar";
#else
            return calendar is GregorianCalendar;
#endif
        }

        private string ReplaceDateFields(string pattern, DateTimeFormatInfo format)
        {
            var result = new StringBuilder();
            for (var i = 0; i < pattern.Length; i++)
            {
                var c = pattern[i];
                if (c == '\\' && i + 1 < pattern.Length)
                {
                    result.Append(c).Append(pattern[++i]);
                }
                else if (c == '\'' || c == '"')
                {
                    // Copy the quoted literal with its quotes
                    result.Append(c);
                    while (++i < pattern.Length && pattern[i] != c)
                    {
                        if (pattern[i] == '\\' && i + 1 < pattern.Length)
                            result.Append(pattern[i++]);
                        result.Append(pattern[i]);
                    }

                    if (i < pattern.Length)
                        result.Append(c);
                }
                else if (c == '%' && i + 1 < pattern.Length && (pattern[i + 1] == 'd' || pattern[i + 1] == 'M' || pattern[i + 1] == 'y'))
                {
                    // The field becomes a literal, so it needs no single character marker
                }
                else if (c == 'd' || c == 'M' || c == 'y')
                {
                    var length = 1;
                    while (i + length < pattern.Length && pattern[i + length] == c)
                        length++;

                    AppendLiteral(result, FormatField(pattern, i, c, length, format));
                    i += length - 1;
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private string FormatField(string pattern, int index, char field, int length, DateTimeFormatInfo format)
        {
            if (field == 'd')
            {
                if (length <= 2)
                    return Day.ToString("D" + length, CultureInfo.InvariantCulture);
                return length == 3 ? format.AbbreviatedDayNames[(int)DayOfWeek] : format.DayNames[(int)DayOfWeek];
            }

            if (field == 'M')
            {
                if (length <= 2)
                    return Month.ToString("D" + length, CultureInfo.InvariantCulture);
                var genitive = IsGenitiveMonth(pattern, index, length);
                if (length == 3)
                    return (genitive ? format.AbbreviatedMonthGenitiveNames : format.AbbreviatedMonthNames)[Month - 1];
                return (genitive ? format.MonthGenitiveNames : format.MonthNames)[Month - 1];
            }

            var year = length <= 2 ? Year % 100 : Year;
            return year.ToString("D" + length, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Like .NET, a month name uses the genitive form when the closest day field before or after it is "d" or "dd".
        /// </summary>
        private static bool IsGenitiveMonth(string pattern, int index, int length)
        {
            var i = index - 1;
            while (i >= 0 && pattern[i] != 'd')
                i--;
            if (i >= 0)
            {
                var repeat = 0;
                while (--i >= 0 && pattern[i] == 'd')
                    repeat++;
                if (repeat <= 1)
                    return true;
            }

            i = index + length;
            while (i < pattern.Length && pattern[i] != 'd')
                i++;
            if (i < pattern.Length)
            {
                var repeat = 0;
                while (++i < pattern.Length && pattern[i] == 'd')
                    repeat++;
                if (repeat <= 1)
                    return true;
            }

            return false;
        }

        private static void AppendLiteral(StringBuilder result, string text)
        {
            foreach (var c in text)
                result.Append('\\').Append(c);
        }

        public static bool TryConvert(object value, bool isDate1904, CultureInfo culture, out ExcelDateTime result)
        {
            if (value is double doubleValue)
            {
                // NaN, infinity and serials outside the DateTime range have no date
                if (double.IsNaN(doubleValue) || Math.Abs(doubleValue) > 1e7)
                {
                    result = null;
                    return false;
                }

                result = new ExcelDateTime(doubleValue, isDate1904);
                return true;
            }
            if (value is int intValue)
            {
                result = new ExcelDateTime(intValue, isDate1904);
                return true;
            }
            if (value is short shortValue)
            {
                result = new ExcelDateTime(shortValue, isDate1904);
                return true;
            }
            else if (value is DateTime dateTimeValue)
            {
                result = new ExcelDateTime(dateTimeValue);
                return true;
            }

            result = null;
            return false;
        }

        // From DateTime class to enable OADate in PCL
        // Number of 100ns ticks per time unit
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;

        private const int MillisPerSecond = 1000;
        private const int MillisPerMinute = MillisPerSecond * 60;
        private const int MillisPerHour = MillisPerMinute * 60;
        private const int MillisPerDay = MillisPerHour * 24;

        // Number of days in a non-leap year
        private const int DaysPerYear = 365;

        // Number of days in 4 years
        private const int DaysPer4Years = DaysPerYear * 4 + 1;

        // Number of days in 100 years
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;

        // Number of days in 400 years
        private const int DaysPer400Years = DaysPer100Years * 4 + 1;

        // Number of days from 1/1/0001 to 12/30/1899
        private const int DaysTo1899 = DaysPer400Years * 4 + DaysPer100Years * 3 - 367;

        private const long DoubleDateOffset = DaysTo1899 * TicksPerDay;

        internal static long DoubleDateToTicks(double value)
        {
            long millis = (long)(value * MillisPerDay + (value >= 0 ? 0.5 : -0.5));

            // The interesting thing here is when you have a value like 12.5 it all positive 12 days and 12 hours from 01/01/1899
            // However if you a value of -12.25 it is minus 12 days but still positive 6 hours, almost as though you meant -11.75 all negative
            // This line below fixes up the millis in the negative case
            if (millis < 0)
            {
                millis -= millis % MillisPerDay * 2;
            }

            millis += DoubleDateOffset / TicksPerMillisecond;
            return millis * TicksPerMillisecond;
        }
    }
}
