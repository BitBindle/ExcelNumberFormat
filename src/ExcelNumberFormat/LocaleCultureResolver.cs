using System.Globalization;

namespace ExcelNumberFormat
{
    /// <summary>
    /// Gives the culture of a locale code in a number format, for example of [$-407] or [$€-407].
    /// </summary>
    /// <param name="localeId">The Windows locale id (LCID) of the locale code: its low 16 bits, for example 0x407.</param>
    /// <returns>The culture that gives the month names, day names and AM/PM designators, or null to let the library look up the culture.</returns>
    public delegate CultureInfo LocaleCultureResolver(int localeId);
}
