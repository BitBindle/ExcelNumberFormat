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

        public bool IsLongSystemDate => LocaleId == 0xF800;

        public bool IsLongSystemTime => LocaleId == 0xF400;

        /// <summary>
        /// If IsLongSystemTime is true, then contains the tokens for the time formatter.
        /// </summary>
        public List<string> TimeTokens { get; set; }

        private static readonly object NoCulture = new object();

        private object culture;

        /// <summary>
        /// The culture of the language of the locale id, or null if the language is not known.
        /// </summary>
        public CultureInfo Culture
        {
            get
            {
                // A section is formatted many times, so look up the culture only once
                var result = culture;
                if (result == null)
                {
                    result = (object)GetCulture(LocaleId) ?? NoCulture;
                    culture = result;
                }

                return result as CultureInfo;
            }
        }

        /// <summary>
        /// The culture that the resolver of the caller gives for the language of the locale id, else <see cref="Culture"/>.
        /// The system date and time codes have no language, so they do not call the resolver.
        /// </summary>
        public CultureInfo ResolveCulture(LocaleCultureResolver cultureOfLocaleId)
        {
            // The resolver can be different for each call, so its result is not kept
            var language = LocaleId & 0xFFFF;
            if (cultureOfLocaleId != null && language != 0 && !IsLongSystemDate && !IsLongSystemTime)
            {
                var result = cultureOfLocaleId(language);
                if (result != null)
                    return result;
            }

            return Culture;
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
