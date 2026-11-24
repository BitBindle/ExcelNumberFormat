using System.Globalization;

namespace ExcelNumberFormat
{
    /// <summary>
    /// Gives the culture of a locale code in a number format, for example of [$-407], [$€-407] or [$-de-DE].
    /// </summary>
    /// <param name="localeId">The Windows locale id (LCID) of the locale code: its low 16 bits, for example 0x407. 0 for a language tag.</param>
    /// <param name="localeName">The language tag of the locale code, for example de-DE for [$-de-DE], [$-de_DE] or [$-de-DE.utf8], or de for [$-de@euro] and [$-de,1]. Null for a hexadecimal locale id.</param>
    /// <param name="culture">The culture of the call to Format. A resolver can return it for a locale code that it does not know.</param>
    /// <returns>The culture that gives the month names, day names and AM/PM designators, or null to let the library look up the culture by its id or tag.</returns>
    /// <remarks>
    /// The resolver must not throw. If it throws an <see cref="System.ArgumentException"/>, Format returns the value as text, as for a value that it cannot convert. Other exceptions go to the caller of Format.
    /// </remarks>
    public delegate CultureInfo LocaleCultureResolver(int localeId, string localeName, CultureInfo culture);
}
