using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExcelNumberFormat
{
    /// <summary>
    /// Parse ECMA-376 number format strings and format values like Excel and other spreadsheet softwares.
    /// </summary>
    public class NumberFormat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumberFormat"/> class.
        /// </summary>
        /// <param name="formatString">The number format string.</param>
        public NumberFormat(string formatString)
        {
            var sections = Parser.ParseSections(formatString, out bool syntaxError);

            IsValid = !syntaxError;
            FormatString = formatString;

            if (IsValid)
            {
                Sections = sections;
                IsDateTimeFormat = Evaluator.GetFirstSection(Sections, SectionType.Date) != null;
                IsTimeSpanFormat = Evaluator.GetFirstSection(Sections, SectionType.Duration) != null;
                IsPercentFormat = Sections.Exists(section => section.IsPercent);
            }
            else
            {
                Sections = new List<Section>();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the number format string is valid.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the number format string.
        /// </summary>
        public string FormatString { get; }

        /// <summary>
        /// Gets a value indicating whether the format represents a DateTime
        /// </summary>
        public bool IsDateTimeFormat { get; }

        /// <summary>
        /// Gets a value indicating whether the format represents a TimeSpan
        /// </summary>
        public bool IsTimeSpanFormat { get; }

        private static volatile LocaleCultureResolver defaultLocaleCultureResolver;

        /// <summary>
        /// Gets or sets the resolver that gives the culture of a locale code such as [$-407] for each call of <see cref="Format(object, CultureInfo, bool)"/>, and for each call of <see cref="Format(object, CultureInfo, bool, LocaleCultureResolver)"/> without a resolver.
        /// It applies to all instances in the process, also in libraries that call Format. The default is null: the library looks up the culture by its id or tag.
        /// The resolver must not throw: see <see cref="LocaleCultureResolver"/>.
        /// The library does not keep the results, so a new value applies to the next call. The resolver must be safe to call from many threads.
        /// </summary>
        public static LocaleCultureResolver DefaultLocaleCultureResolver
        {
            get { return defaultLocaleCultureResolver; }
            set { defaultLocaleCultureResolver = value; }
        }

        /// <summary>
        /// Gets a value indicating whether a section of the format shows numbers as a percentage: a number, fraction or scientific section has a % outside quotes.
        /// </summary>
        public bool IsPercentFormat { get; }

        /// <summary>
        /// Gets the parts of a date and time that the format shows: the parts of the first section that shows a date, a time or an elapsed time.
        /// <see cref="ExcelNumberFormat.DateTimeParts.None"/> if no section shows one.
        /// </summary>
        public DateTimeParts DateTimeParts
        {
            get
            {
                // Most callers do not read the parts, so find them on the first read. Two threads can both find them, with the same result.
                var parts = dateTimeParts;
                if (parts < 0)
                {
                    var section = Sections.Find(s => s.Type == SectionType.Date || s.Type == SectionType.Duration);
                    parts = section != null ? (int)Formatter.GetDateTimeParts(section) : 0;
                    dateTimeParts = parts;
                }

                return (DateTimeParts)parts;
            }
        }

        private int dateTimeParts = -1;

        internal List<Section> Sections { get; }

        /// <summary>
        /// Gets the color of the section that formats a value. This is the section that <see cref="Format(object, CultureInfo, bool)"/> uses for the value, by its sign, its type and the conditions of the format. The color is also given when Format cannot convert the value and returns it as text.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>The color of the section, or null if the section has no color, no section formats the value, or the format is not valid.</returns>
        public NumberFormatColor GetColor(object value)
        {
            var section = Evaluator.GetSection(Sections, CompatibleConvert.Normalize(value));
            return section?.Color;
        }

        /// <summary>
        /// Formats a value with this number format in a specified culture.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">The culture to use for formatting.</param>
        /// <param name="isDate1904">If false, numeric dates start on January 0 1900 and include February 29 1900 - like Excel on PC. If true, numeric dates start on January 1 1904 - like Excel on Mac.</param>
        /// <returns>The formatted string.</returns>
        public string Format(object value, CultureInfo culture, bool isDate1904 = false)
        {
            return Format(value, culture, isDate1904, null);
        }

        /// <summary>
        /// Formats a value with this number format in a specified culture, with the cultures of locale codes from a resolver.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">The culture to use for formatting.</param>
        /// <param name="isDate1904">If false, numeric dates start on January 0 1900 and include February 29 1900 - like Excel on PC. If true, numeric dates start on January 1 1904 - like Excel on Mac.</param>
        /// <param name="localeCultureResolver">Gives the culture of a locale code such as [$-407] or [$-de-DE] for the month names, day names and AM/PM designators. If it is null, the library uses <see cref="DefaultLocaleCultureResolver"/>. If the resolver gives null, the library looks up the culture by its id or tag. The library does not keep the result. The resolver must not throw: see <see cref="LocaleCultureResolver"/>.</param>
        /// <returns>The formatted string.</returns>
        public string Format(object value, CultureInfo culture, bool isDate1904, LocaleCultureResolver localeCultureResolver)
        {
            var normalized = CompatibleConvert.Normalize(value);
            var section = Evaluator.GetSection(Sections, normalized);
            if (section == null)
                return CompatibleConvert.ToString(value, culture);

            try
            {
                return Formatter.Format(normalized, section, culture, isDate1904, localeCultureResolver ?? defaultLocaleCultureResolver);
            }
            catch (InvalidCastException)
            {
                // TimeSpan cast exception
                return CompatibleConvert.ToString(value, culture);
            }
            catch (FormatException)
            {
                // Convert.ToDouble/ToDateTime exceptions
                return CompatibleConvert.ToString(value, culture);
            }
            catch (OverflowException)
            {
                // Values out of range for the format, such as an infinite duration
                return CompatibleConvert.ToString(value, culture);
            }
            catch (ArgumentException)
            {
                // Includes ArgumentOutOfRangeException from DateTime and TimeSpan
                return CompatibleConvert.ToString(value, culture);
            }
        }
    }
}
