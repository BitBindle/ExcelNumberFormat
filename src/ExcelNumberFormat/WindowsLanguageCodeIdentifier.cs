using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExcelNumberFormat
{
    /// <summary>
    /// Represents a Windows Language Code Identifier (LCID) as defined by [MS-LCID] (https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/70feba9f-294e-491e-b6eb-56532684c37f)
    /// </summary>
    internal class WindowsLanguageCodeIdentifier
    {
        public int LocaleId { get; set; }

        /// <summary>
        /// The language tag of a locale code such as [$-de-DE], or null for a code that is only a hexadecimal locale id. <see cref="LocaleId"/> is 0 for a tag.
        /// A code such as [$-de] is both a locale id and a tag.
        /// </summary>
        public string LocaleName { get; set; }

        public bool IsLongSystemDate => LocaleId == 0xF800;

        public bool IsLongSystemTime => LocaleId == 0xF400;

        /// <summary>
        /// The code names a language: a locale id or a language tag.
        /// </summary>
        public bool HasLocale => LocaleId != 0 || LocaleName != null;

        /// <summary>
        /// If IsLongSystemTime is true, then contains the tokens for the time formatter.
        /// </summary>
        public List<string> TimeTokens { get; set; }

        private static readonly object NoCulture = new object();

        private object culture;

        /// <summary>
        /// The culture of the language of the locale id or tag, or null if the language is not known.
        /// </summary>
        public CultureInfo Culture
        {
            get
            {
                // A section is formatted many times, so look up the culture only once
                var result = culture;
                if (result == null)
                {
                    // The id comes first. A code such as [$-de] is also a tag, which gives the culture if the id does not.
                    result = (object)(GetCulture(LocaleId) ?? (LocaleName != null ? GetCulture(LocaleName) : null)) ?? NoCulture;
                    culture = result;
                }

                return result as CultureInfo;
            }
        }

        /// <summary>
        /// Reads a language tag such as de-DE, de_DE, de@euro, de.utf8 or de-DE,1. Returns the tag without the codeset, the modifier and the calendar, with "-" between the parts, or null if the text is not a tag.
        /// </summary>
        public static string ParseLocaleName(string text)
        {
            // A tag starts with a letter. Most locale ids, such as 407, start with a digit.
            if (text.Length == 0 || !IsLetter(text[0]))
                return null;

            // Codesets and modifiers come from POSIX locale names: de_DE.UTF-8, de@euro. A calendar and number system can follow a comma: de-DE,1
            var end = text.IndexOfAny(new[] { '.', '@', ',' });
            var name = (end >= 0 ? text.Substring(0, end) : text).Replace('_', '-');

            var parts = name.Split('-');
            if (parts[0].Length < 2 || parts[0].Length > 3 || !IsLetters(parts[0]))
                return null;

            for (var i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length < 1 || parts[i].Length > 8 || !IsLettersOrDigits(parts[i]))
                    return null;
            }

            return name;
        }

        private static bool IsLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private static bool IsLetters(string text)
        {
            foreach (var c in text)
            {
                if (!IsLetter(c))
                    return false;
            }

            return true;
        }

        private static bool IsLettersOrDigits(string text)
        {
            foreach (var c in text)
            {
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9'))
                    return false;
            }

            return true;
        }

        private static CultureInfo GetCulture(int lcid)
        {
            // The language is in the low 16 bits, the rest selects calendar and numeral systems
            var language = lcid & 0xFFFF;
            if (language == 0)
                return null;

#if NETSTANDARD1_0
            // Cultures cannot be looked up by id
            return null;
#else
            CultureInfo culture;
            try
            {
                culture = new CultureInfo(language);
            }
            catch (ArgumentException)
            {
                return null;
            }

            return WithGregorianCalendar(culture);
#endif
        }

        private static CultureInfo GetCulture(string name)
        {
            // "und" is the BCP 47 code of an undetermined language, and .NET gives it the invariant culture
            if (name.Length >= 3 && string.Compare(name, 0, "und", 0, 3, StringComparison.OrdinalIgnoreCase) == 0 && (name.Length == 3 || name[3] == '-'))
                return null;

            CultureInfo culture;
            try
            {
                culture = new CultureInfo(name);
            }
            catch (ArgumentException)
            {
                return null;
            }

#if !NETSTANDARD1_0
            // ICU gives a culture for any well-formed tag. The id of a language that it does not know is LOCALE_CUSTOM_UNSPECIFIED.
            var language = culture;
            while (language.Parent.Name.Length > 0)
                language = language.Parent;

            if (language.LCID == LocaleCustomUnspecified)
                return null;
#endif

            return WithGregorianCalendar(culture);
        }

        private const int LocaleCustomUnspecified = 0x1000;

        private static CultureInfo WithGregorianCalendar(CultureInfo culture)
        {
#if NETSTANDARD1_0
            // The calendar types are not available
            return culture.DateTimeFormat.Calendar.GetType().Name == "GregorianCalendar" ? culture : null;
#else
            if (culture.DateTimeFormat.Calendar is GregorianCalendar)
                return culture;

            // The month of the date is a Gregorian month, so the names must come from a Gregorian calendar of the language
            GregorianCalendar gregorian = null;
            foreach (var calendar in culture.OptionalCalendars)
            {
                if (calendar is GregorianCalendar optional && (gregorian == null || optional.CalendarType == GregorianCalendarTypes.Localized))
                    gregorian = optional;
            }

            if (gregorian == null)
                return null;

            culture = (CultureInfo)culture.Clone();
            culture.DateTimeFormat.Calendar = gregorian;
            return culture;
#endif
        }
    }
}
