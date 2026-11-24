using System;
using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// A locale code such as [$-407] selects the language of the month and day names, like it selects the AM/PM designators.
// The separators still come from the culture of the call.

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class LocaleNameTests
    {
        // Friday 10 March 2023
        private const double Serial = 44995;

        string Format(object value, string formatString, CultureInfo culture)
        {
            var format = new NumberFormat(formatString);
            if (format.IsValid)
                return format.Format(value, culture);

            return null;
        }

        void Test(object value, string format, string expected)
        {
            var result = Format(value, format, CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestLocaleCodeSelectsMonthNames()
        {
            var de = new CultureInfo("de-DE");
            Test(Serial, "[$-407]mmmm", "März");
            Test(Serial, "[$-407]mmm", de.DateTimeFormat.AbbreviatedMonthNames[2]);
            Test(Serial, "[$-407]mmmmm", "M");
            Test(Serial, "[$-40C]mmmm", "mars");
            Test(Serial, "[$-407]d. mmmm yyyy", "10. März 2023");
        }

        [TestMethod]
        public void TestLocaleCodeSelectsDayNames()
        {
            var de = new CultureInfo("de-DE");
            Test(Serial, "[$-407]dddd", "Freitag");
            Test(Serial, "[$-407]ddd", de.DateTimeFormat.AbbreviatedDayNames[5]);
            Test(Serial, "[$-40C]dddd", "vendredi");
            Test(Serial, "[$-411]dddd", "金曜日");
        }

        [TestMethod]
        public void TestLocaleCodeWinsOverCulture()
        {
            var de = new CultureInfo("de-DE");
            Assert.AreEqual("Friday, March 10, 2023", Format(Serial, "[$-409]dddd, mmmm d, yyyy", de));
            Assert.AreEqual("vendredi 10 mars", Format(Serial, "[$-40C]dddd d mmmm", de));
        }

        [TestMethod]
        public void TestLocaleCodeOfCultureWithOtherCalendarSelectsGregorianNames()
        {
            // The default calendar of ar-SA is Um Al Qura and of fa-IR is Persian, but the date is a Gregorian date
            Test(Serial, "[$-401]mmmm yyyy", "مارس 2023");
            Test(Serial, "[$-401]dddd", "الجمعة");
            Test(Serial, "[$-429]mmmm", "مارس");
            Test(Serial, "[$-41E]mmmm", "มีนาคม");
        }

        [TestMethod]
        public void TestLocaleCodeWithCalendarAndNumeralBits()
        {
            // The language is in the low 16 bits of the locale code
            Test(Serial, "[$-1010407]mmmm", "März");
        }

        [TestMethod]
        public void TestCurrencyLocaleCodeSelectsNames()
        {
            Test(Serial, "[$€-407]mmmm", "€März");
        }

        [TestMethod]
        public void TestLocaleCodeKeepsSeparatorsOfCulture()
        {
            var en = new CultureInfo("en-US");
            Assert.AreEqual("10/03/2023", Format(Serial, "[$-407]dd/mm/yyyy", en));
            Assert.AreEqual("1,234.50", Format(1234.5, "[$-407]#,##0.00", en));
            Assert.AreEqual("1,234.50", Format(1234.5, "[$-407]#,##0.00", CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void TestUnknownLocaleCodeKeepsCulture()
        {
            var de = new CultureInfo("de-DE");
            Assert.AreEqual("März", Format(Serial, "[$-0]mmmm", de));
            Assert.AreEqual("März", Format(Serial, "[$-FFFF]mmmm", de));
            Test(Serial, "[$-FFFF]dddd", "Friday");
        }

        [TestMethod]
        public void TestCultureSelectsNamesWithoutLocaleCode()
        {
            var de = new CultureInfo("de-DE");
            Assert.AreEqual("Freitag, 10. März 2023", Format(Serial, "dddd, d. mmmm yyyy", de));
            Test(Serial, "dddd, mmmm d, yyyy", "Friday, March 10, 2023");
        }
    }
}
