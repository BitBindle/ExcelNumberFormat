using System;
using System.Globalization;

namespace ExcelNumberFormat
{
    /// <summary>
    /// The color of a section of a number format, for example [Red] or [Color10].
    /// </summary>
    public sealed class NumberFormatColor
    {
        // The color names in the order of the first eight colors of the default palette
        private static readonly string[] Names = { "Black", "White", "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan" };

        private NumberFormatColor(string name, int index)
        {
            Name = name;
            Index = index;
        }

        /// <summary>
        /// Gets the color name: Black, Blue, Cyan, Green, Magenta, Red, White or Yellow. Null for [Color1] to [Color56].
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the index of the color in the palette of the workbook, from 1 to 56.
        /// A color name has the index of the same color in the default palette: Black 1, White 2, Red 3, Green 4, Blue 5, Yellow 6, Magenta 7 and Cyan 8.
        /// </summary>
        /// <remarks>
        /// A workbook can change its palette, so the library does not give an RGB value.
        /// ECMA-376 Part 1, 18.8.27 (indexedColors) gives the default palette: palette index n is indexed color n + 7.
        /// </remarks>
        public int Index { get; }

        /// <summary>
        /// Returns the color as in the number format, without brackets: for example Red or Color10.
        /// </summary>
        public override string ToString()
        {
            return Name ?? "Color" + Index.ToString(CultureInfo.InvariantCulture);
        }

        internal static NumberFormatColor Parse(string expression)
        {
            for (var i = 0; i < Names.Length; i++)
            {
                if (string.Compare(expression, Names[i], StringComparison.OrdinalIgnoreCase) == 0)
                    return new NumberFormatColor(Names[i], i + 1);
            }

            const string prefix = "Color";
            if (expression.Length > prefix.Length
                && string.Compare(expression, 0, prefix, 0, prefix.Length, StringComparison.OrdinalIgnoreCase) == 0
                && int.TryParse(expression.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                && index >= 1 && index <= 56)
            {
                return new NumberFormatColor(null, index);
            }

            return null;
        }
    }
}
