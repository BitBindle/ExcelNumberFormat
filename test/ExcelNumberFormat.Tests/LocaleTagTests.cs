using System;
using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// Excel writes a locale code as a hexadecimal locale id, such as [$-407]. Other spreadsheet programs and files also
// contain language tags, such as [$-de-DE], or POSIX locale names, such as [$-de_DE.utf8] and [$-de@euro].
// A tag selects the language of the month names, day names and AM/PM designators, as a locale id does.

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class LocaleTagTests
    {
        // Friday 10 March 2023, 06:00
        private const double Serial = 44995.25;

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
        public void TestLanguageTagSelectsNames()
        {
            var de = new CultureInfo("de-DE");
            Test(Serial, "[$-de-DE]mmmm", "März");
            Test(Serial, "[$-de-DE]mmm", de.DateTimeFormat.AbbreviatedMonthNames[2]);
            Test(Serial, "[$-de-DE]dddd, d. mmmm yyyy", "Freitag, 10. März 2023");
            Test(Serial, "[$-fr-CA]dddd d mmmm", "vendredi 10 mars");
            Test(Serial, "[$-fr]mmmm", "mars");
            Test(Serial, "[$-ja-JP]h:mm AM/PM", "6:00 午前");
        }

        [TestMethod]
        public void TestPosixLocaleNameSelectsNames()
        {
            // The codeset and the modifier do not change the language
            var de = new CultureInfo("de-DE");
            Test(Serial, "[$-de@euro]dddd d mmmm yyyy", "Freitag 10 März 2023");
            Test(Serial, "[$-de.utf8]ddd d mmm yyyy", de.DateTimeFormat.AbbreviatedDayNames[5] + " 10 " + de.DateTimeFormat.AbbreviatedMonthNames[2] + " 2023");
            Test(Serial, "[$-de_DE]mmmm", "März");
            Test(Serial, "[$-de_DE.UTF-8]mmmm", "März");
            Test(Serial, "[$-de-DE,1]mmmm", "März");
        }

        [TestMethod]
        public void TestLanguageTagWinsOverCulture()
        {
            Assert.AreEqual("Friday, March 10, 2023", Format(Serial, "[$-en-US]dddd, mmmm d, yyyy", new CultureInfo("de-DE")));
        }

        [TestMethod]
        public void TestLanguageTagKeepsSeparatorsOfCulture()
        {
            var en = new CultureInfo("en-US");
            Assert.AreEqual("1,234.50", Format(1234.5, "[$-fr-CA]#,##0.00", en));
            Assert.AreEqual("10/mars/2023", Format(Serial, "[$-fr-CA]dd/mmmm/yyyy", en));
        }

        [TestMethod]
        public void TestLanguageTagUsesDesignatorsForAP()
        {
            // As with a locale id, A/P shows the AM/PM designator of the language
            Test(Serial, "[$-ja-JP]h A/P", "6 午前");
        }

        [TestMethod]
        public void TestLanguageTagInCurrencyCode()
        {
            Test(Serial, "[$€-de-DE]mmmm", "€März");
        }

        [TestMethod]
        public void TestHexadecimalCodeStaysLocaleId()
        {
            // "de" and "ca" are also hexadecimal numbers, so they stay locale ids
            Test(Serial, "[$-de]mmmm", "March");
            Test(Serial, "[$-ca]mmmm", "March");
            Test(Serial, "[$-407]mmmm", "März");
        }

        [TestMethod]
        public void TestUnknownLanguageTagKeepsCulture()
        {
            var de = new CultureInfo("de-DE");
            Assert.AreEqual("März", Format(Serial, "[$-zz-ZZ]mmmm", de));
            Assert.AreEqual("März", Format(Serial, "[$-qqq]mmmm", de));
        }

        [TestMethod]
        public void TestTextThatIsNotTagIsIgnored()
        {
            // These codes are not tags, as before
            Test(Serial, "[$-x]mmmm", "March");
            Test(Serial, "[$-de!]mmmm", "March");
            Test(Serial, "[$-1-2]mmmm", "March");
        }
    }
}
