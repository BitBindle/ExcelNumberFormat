using System.Collections.Generic;

namespace ExcelNumberFormat
{
    internal class Section
    {
        public int SectionIndex { get; set; }

        public SectionType Type { get; set; }

        public NumberFormatColor Color { get; set; }

        public Condition Condition { get; set; }

        /// <summary>
        /// The section formats numbers and has a % outside quotes.
        /// </summary>
        public bool IsPercent { get; set; }

        public ExponentialSection Exponential { get; set; }

        public FractionSection Fraction { get; set; }

        public DecimalSection Number { get; set; }

        public List<string> GeneralTextDateDurationParts { get; set; }

        public WindowsLanguageCodeIdentifier Lcid { get; set; }
    }
}