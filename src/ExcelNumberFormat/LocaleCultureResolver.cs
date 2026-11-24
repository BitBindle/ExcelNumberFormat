using System.Globalization;

namespace ExcelNumberFormat
{
    /// <summary>
    /// Gives the culture of a locale code in a number format, for example of [$-407] or [$€-407].
    /// </summary>
    /// <param name="localeId">The Windows locale id (LCID) of the locale code: its low 16 bits, for example 0x407.</param>
    /// <param name="culture">The culture of the call to Format. A resolver can return it for a locale code that it does not know.</param>
    /// <returns>The culture that gives the month names, day names and AM/PM designators, or null to let the library look up the culture by its id.</returns>
    /// <remarks>
    /// The resolver must not throw. If it throws an <see cref="System.ArgumentException"/>, Format returns the value as text, as for a value that it cannot convert. Other exceptions go to the caller of Format.
    /// </remarks>
    public delegate CultureInfo LocaleCultureResolver(int localeId, CultureInfo culture);
}
