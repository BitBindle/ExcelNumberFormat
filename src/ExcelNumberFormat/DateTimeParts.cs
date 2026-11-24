using System;

namespace ExcelNumberFormat
{
    /// <summary>
    /// The parts of a date and time that a number format shows.
    /// </summary>
    [Flags]
    public enum DateTimeParts
    {
        /// <summary>The format shows no part of a date or time.</summary>
        None = 0,

        /// <summary>The year: y, yy, yyyy, or the era year e.</summary>
        Year = 1,

        /// <summary>The month: m, mm, mmm, mmmm or mmmmm, if it is not a minute.</summary>
        Month = 2,

        /// <summary>The day: d or dd, or the weekday ddd or dddd.</summary>
        Day = 4,

        /// <summary>The hour: h, hh or [h].</summary>
        Hour = 8,

        /// <summary>The minute: m or mm after an hour or before a second, or [m].</summary>
        Minute = 16,

        /// <summary>The second: s, ss, [s], or a fraction of a second such as .00.</summary>
        Second = 32,
    }
}
