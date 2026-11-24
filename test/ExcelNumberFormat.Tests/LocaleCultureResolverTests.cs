using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// An application can give the culture of a locale code such as [$-407] itself. This is necessary when .NET cannot
// look up the culture by its id: with invariant globalization, with a reduced ICU data set (for example in the
// browser), on netstandard1.0, or when the application must show other names than .NET, for example the names of Excel.

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class LocaleCultureResolverTests
    {
        // Friday 10 March 2023, 06:00
        private const double Serial = 44995.25;

        // German names as Excel shows them. .NET shows "März" for mmm.
        private static readonly CultureInfo ExcelGerman = CreateCulture(
            "Januar;Februar;März;April;Mai;Juni;Juli;August;September;Oktober;November;Dezember",
            "Jan;Feb;Mrz;Apr;Mai;Jun;Jul;Aug;Sep;Okt;Nov;Dez",
            "Sonntag;Montag;Dienstag;Mittwoch;Donnerstag;Freitag;Samstag",
            "So;Mo;Di;Mi;Do;Fr;Sa",
            "vorm.",
            "nachm.");

        private static CultureInfo CreateCulture(string months, string shortMonths, string days, string shortDays, string am, string pm)
        {
            // A culture from fixed data, without a lookup of the culture by its id
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.MonthNames = (months + ";").Split(';');
            culture.DateTimeFormat.AbbreviatedMonthNames = (shortMonths + ";").Split(';');
            culture.DateTimeFormat.DayNames = days.Split(';');
            culture.DateTimeFormat.AbbreviatedDayNames = shortDays.Split(';');
            culture.DateTimeFormat.AMDesignator = am;
            culture.DateTimeFormat.PMDesignator = pm;
            culture.DateTimeFormat.DateSeparator = ".";
            return CultureInfo.ReadOnly(culture);
        }

        private static CultureInfo ResolveGerman(int localeId, string localeName, CultureInfo culture)
        {
            return localeId == 0x407 ? ExcelGerman : null;
        }

        string Format(object value, string formatString, CultureInfo culture, LocaleCultureResolver localeCultureResolver)
        {
            var format = new NumberFormat(formatString);
            if (format.IsValid)
                return format.Format(value, culture, false, localeCultureResolver);

            return null;
        }

        void Test(object value, string format, string expected)
        {
            var result = Format(value, format, CultureInfo.InvariantCulture, ResolveGerman);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestResolverGivesMonthAndDayNames()
        {
            Test(Serial, "[$-407]d. mmm yyyy", "10. Mrz 2023");
            Test(Serial, "[$-407]mmmm", "März");
            Test(Serial, "[$-407]mmmmm", "M");
            Test(Serial, "[$-407]dddd", "Freitag");
            Test(Serial, "[$-407]ddd", "Fr");
            Test(new DateTime(2023, 3, 10), "[$-407]ddd, d. mmm", "Fr, 10. Mrz");
        }

        [TestMethod]
        public void TestResolverGivesAmPmDesignators()
        {
            Test(Serial, "[$-407]h:mm AM/PM", "6:00 vorm.");
            Test(Serial + 0.5, "[$-407]h:mm AM/PM", "6:00 nachm.");
            Test(Serial, "[$-407]h:mm A/P", "6:00 vorm.");
        }

        [TestMethod]
        public void TestResolverGetsLanguageOfLocaleCode()
        {
            var ids = new List<int>();
            CultureInfo Collect(int localeId, string localeName, CultureInfo culture)
            {
                ids.Add(localeId);
                return ExcelGerman;
            }

            Assert.AreEqual("Mrz", Format(Serial, "[$-407]mmm", CultureInfo.InvariantCulture, Collect));
            Assert.AreEqual("€Mrz", Format(Serial, "[$€-407]mmm", CultureInfo.InvariantCulture, Collect));
            Assert.AreEqual("Mrz", Format(Serial, "[$-1010407]mmm", CultureInfo.InvariantCulture, Collect));
            Assert.AreEqual("Mrz", Format(Serial, "[$-C07]mmm", CultureInfo.InvariantCulture, Collect));
            CollectionAssert.AreEqual(new[] { 0x407, 0x407, 0x407, 0xC07 }, ids);
        }

        [TestMethod]
        public void TestResolverGetsCultureOfCall()
        {
            var en = new CultureInfo("en-US");
            var cultures = new List<CultureInfo>();
            CultureInfo Collect(int localeId, string localeName, CultureInfo culture)
            {
                cultures.Add(culture);
                return null;
            }

            Format(Serial, "[$-407]mmm", en, Collect);
            Format(Serial, "[$-407]mmm", CultureInfo.InvariantCulture, Collect);
            CollectionAssert.AreEqual(new[] { en, CultureInfo.InvariantCulture }, cultures);
        }

        [TestMethod]
        public void TestResolverReturnsCultureOfCallForUnknownCode()
        {
            // The resolver keeps the names of the call for a code that it does not know, so the library does not look up a culture by its id
            var en = new CultureInfo("en-US");
            CultureInfo GermanOrCall(int localeId, string localeName, CultureInfo culture)
            {
                return localeId == 0x407 ? ExcelGerman : culture;
            }

            Assert.AreEqual("March", Format(Serial, "[$-40C]mmmm", en, GermanOrCall));
            Assert.AreEqual("Friday", Format(Serial, "[$-411]dddd", en, GermanOrCall));
            Assert.AreEqual("Mrz", Format(Serial, "[$-407]mmm", en, GermanOrCall));
        }

        [TestMethod]
        public void TestResolverGetsLanguageTag()
        {
            var calls = new List<string>();
            CultureInfo Collect(int localeId, string localeName, CultureInfo culture)
            {
                calls.Add(localeId.ToString("X", CultureInfo.InvariantCulture) + " " + (localeName ?? "null"));
                return localeName == "de-DE" || localeName == "de" ? ExcelGerman : culture;
            }

            Assert.AreEqual("Mrz", Format(Serial, "[$-de-DE]mmm", CultureInfo.InvariantCulture, Collect));
            Assert.AreEqual("Mrz", Format(Serial, "[$-de_DE.utf8]mmm", CultureInfo.InvariantCulture, Collect));
            Assert.AreEqual("Mrz", Format(Serial, "[$-de@euro]mmm", CultureInfo.InvariantCulture, Collect));
            Assert.AreEqual("€Mrz", Format(Serial, "[$€-de-DE]mmm", CultureInfo.InvariantCulture, Collect));
            Assert.AreEqual("Mar", Format(Serial, "[$-fr-CA]mmm", CultureInfo.InvariantCulture, Collect));
            Assert.AreEqual("Mar", Format(Serial, "[$-407]mmm", CultureInfo.InvariantCulture, Collect));
            CollectionAssert.AreEqual(new[] { "0 de-DE", "0 de-DE", "0 de", "0 de-DE", "0 fr-CA", "407 null" }, calls);
        }

        [TestMethod]
        public void TestResolverIsNotCalledWithoutLanguage()
        {
            CultureInfo Fail(int localeId, string localeName, CultureInfo culture)
            {
                Assert.Fail("Unexpected call for " + localeId.ToString("X", CultureInfo.InvariantCulture));
                return null;
            }

            var en = new CultureInfo("en-US");
            Assert.AreEqual("Mar", Format(Serial, "mmm", en, Fail));
            Assert.AreEqual("Mar", Format(Serial, "[$-0]mmm", en, Fail));
            Assert.AreEqual("Mar", Format(Serial, "[$-10000]mmm", en, Fail));
            Assert.AreEqual(en.DateTimeFormat.AMDesignator, Format(Serial, "[$-F400]AM/PM", en, Fail));
            Assert.AreEqual("Friday, March 10, 2023", Format(Serial, "[$-F800]dddd, mmmm dd, yyyy", en, Fail));
            Assert.AreEqual("1,234.50", Format(1234.5, "[$-407]#,##0.00", en, Fail));
        }

        [TestMethod]
        public void TestResolverResultNullKeepsLookup()
        {
            var de = new CultureInfo("de-DE");
            Assert.AreEqual(de.DateTimeFormat.AbbreviatedMonthNames[2], Format(Serial, "[$-407]mmm", CultureInfo.InvariantCulture, (id, name, culture) => null));
            Assert.AreEqual("mars", Format(Serial, "[$-40C]mmmm", CultureInfo.InvariantCulture, ResolveGerman));
            Assert.AreEqual(de.DateTimeFormat.AbbreviatedMonthNames[2], Format(Serial, "[$-407]mmm", CultureInfo.InvariantCulture, null));
        }

        [TestMethod]
        public void TestResolverKeepsSeparatorsOfCulture()
        {
            var en = new CultureInfo("en-US");
            Assert.AreEqual("10/Mrz/2023", Format(Serial, "[$-407]dd/mmm/yyyy", en, ResolveGerman));
        }

        [TestMethod]
        public void TestResolverOfEachCallIsUsed()
        {
            // The culture of the resolver is not kept in the parsed format
            var format = new NumberFormat("[$-407]mmmm");
            var other = CreateCulture(
                "M1;M2;M3;M4;M5;M6;M7;M8;M9;M10;M11;M12",
                "1;2;3;4;5;6;7;8;9;10;11;12",
                "D0;D1;D2;D3;D4;D5;D6",
                "0;1;2;3;4;5;6",
                "a",
                "p");

            Assert.AreEqual("März", format.Format(Serial, CultureInfo.InvariantCulture, false, ResolveGerman));
            Assert.AreEqual("M3", format.Format(Serial, CultureInfo.InvariantCulture, false, (id, name, culture) => other));
            Assert.AreEqual(new CultureInfo("de-DE").DateTimeFormat.MonthNames[2], format.Format(Serial, CultureInfo.InvariantCulture));
            Assert.AreEqual("M3", format.Format(Serial, CultureInfo.InvariantCulture, false, (id, name, culture) => other));
        }

        [TestMethod]
        public void TestResolverWithDate1904()
        {
            var format = new NumberFormat("[$-407]d. mmm yyyy");
            Assert.AreEqual("10. Mrz 2023", format.Format(Serial - 1462, CultureInfo.InvariantCulture, true, ResolveGerman));
        }
    }
}
