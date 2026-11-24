using System;
using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// A grid or a cell editor needs to know what a format shows, without formatting a value:
// a percent format shows the value as a percentage, and a date format shows some of year, month, day, hour, minute and second.

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class FormatInfoTests
    {
        bool IsPercent(string formatString)
        {
            return new NumberFormat(formatString).IsPercentFormat;
        }

        DateTimeParts Parts(string formatString)
        {
            return new NumberFormat(formatString).DateTimeParts;
        }

        const DateTimeParts Date = DateTimeParts.Year | DateTimeParts.Month | DateTimeParts.Day;

        [TestMethod]
        public void TestPercentFormats()
        {
            Assert.IsTrue(IsPercent("0%"));
            Assert.IsTrue(IsPercent("0.00%"));
            Assert.IsTrue(IsPercent("#,##0.0%"));
            Assert.IsTrue(IsPercent("[Red]0%;[Blue]-0%"));
            Assert.IsTrue(IsPercent("0.00E+00%"));
            Assert.IsTrue(IsPercent("# ?/?%"));
        }

        [TestMethod]
        public void TestPercentInAnySection()
        {
            // As IsDateTimeFormat, each section counts
            Assert.IsTrue(IsPercent("0;-0%"));
            Assert.IsTrue(IsPercent("0;-0;0%"));
            Assert.IsTrue(IsPercent("[>1]0;0%"));
        }

        [TestMethod]
        public void TestNotPercentFormats()
        {
            Assert.IsFalse(IsPercent("0.00"));
            Assert.IsFalse(IsPercent("General"));
            Assert.IsFalse(IsPercent("0\"%\""));
            Assert.IsFalse(IsPercent("0\\%"));
            Assert.IsFalse(IsPercent("@\"%\""));
            Assert.IsFalse(IsPercent("@%"));
            Assert.IsFalse(IsPercent("yyyy%"));
            Assert.IsFalse(IsPercent("0%\""));
        }

        [TestMethod]
        public void TestPercentFormatMultipliesBy100()
        {
            Assert.AreEqual("50%", new NumberFormat("0%").Format(0.5, CultureInfo.InvariantCulture));
            Assert.AreEqual("50%", new NumberFormat("0\"%\"").Format(50, CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void TestDatePartsOfDateFormats()
        {
            Assert.AreEqual(Date, Parts("yyyy-mm-dd"));
            Assert.AreEqual(Date, Parts("m/d/yy"));
            Assert.AreEqual(DateTimeParts.Year | DateTimeParts.Month, Parts("mmm-yy"));
            Assert.AreEqual(DateTimeParts.Month, Parts("mmmm"));
            Assert.AreEqual(DateTimeParts.Day | DateTimeParts.Month, Parts("d-mmm"));
            Assert.AreEqual(DateTimeParts.Day, Parts("dddd"));
            Assert.AreEqual(Date, Parts("[$-F800]dddd, mmmm dd, yyyy"));
            Assert.AreEqual(Date, Parts("[$-411]ggge\"年\"m\"月\"d\"日\""));
        }

        [TestMethod]
        public void TestDatePartsOfTimeFormats()
        {
            Assert.AreEqual(DateTimeParts.Hour | DateTimeParts.Minute, Parts("h:mm"));
            Assert.AreEqual(DateTimeParts.Hour | DateTimeParts.Minute, Parts("h:mm AM/PM"));
            Assert.AreEqual(DateTimeParts.Hour | DateTimeParts.Minute | DateTimeParts.Second, Parts("hh:mm:ss"));
            Assert.AreEqual(DateTimeParts.Minute | DateTimeParts.Second, Parts("mm:ss"));
            Assert.AreEqual(DateTimeParts.Minute | DateTimeParts.Second, Parts("mm:ss.0"));
            Assert.AreEqual(DateTimeParts.Second, Parts("ss.000"));
            Assert.AreEqual(DateTimeParts.Hour, Parts("h AM/PM"));
        }

        [TestMethod]
        public void TestDatePartsOfDateTimeFormats()
        {
            Assert.AreEqual(Date | DateTimeParts.Hour | DateTimeParts.Minute, Parts("m/d/yyyy h:mm"));
            Assert.AreEqual(Date | DateTimeParts.Hour | DateTimeParts.Minute | DateTimeParts.Second, Parts("yyyy-mm-dd hh:mm:ss"));
            Assert.AreEqual(Date | DateTimeParts.Hour | DateTimeParts.Minute | DateTimeParts.Second, Parts("dd.mm.yyyy hh:mm:ss.000"));
        }

        [TestMethod]
        public void TestDatePartsOfElapsedTimeFormats()
        {
            Assert.AreEqual(DateTimeParts.Hour | DateTimeParts.Minute | DateTimeParts.Second, Parts("[h]:mm:ss"));
            Assert.AreEqual(DateTimeParts.Minute | DateTimeParts.Second, Parts("[mm]:ss"));
            Assert.AreEqual(DateTimeParts.Second, Parts("[ss].00"));
        }

        [TestMethod]
        public void TestDatePartsOfFirstDateSection()
        {
            // The first section that shows a date, a time or an elapsed time counts
            Assert.AreEqual(DateTimeParts.Hour | DateTimeParts.Minute, Parts("h:mm;@"));
            Assert.AreEqual(DateTimeParts.Year, Parts("0;yyyy;h:mm"));
            Assert.AreEqual(DateTimeParts.Hour | DateTimeParts.Minute, Parts("[h]:mm;yyyy"));
        }

        [TestMethod]
        public void TestNoDateParts()
        {
            Assert.AreEqual(DateTimeParts.None, Parts("0.00"));
            Assert.AreEqual(DateTimeParts.None, Parts("General"));
            Assert.AreEqual(DateTimeParts.None, Parts("@"));
            Assert.AreEqual(DateTimeParts.None, Parts("0%"));
            Assert.AreEqual(DateTimeParts.None, Parts("\"yyyy\"0"));
            Assert.AreEqual(DateTimeParts.None, Parts("yyyy\""));
        }

        [TestMethod]
        public void TestDatePartsAgreeWithFormat()
        {
            // The minute and month rule of m is the rule of Format
            var value = new DateTime(2023, 3, 10, 14, 5, 0);
            Assert.AreEqual("03", new NumberFormat("mm").Format(value, CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeParts.Month, Parts("mm"));
            Assert.AreEqual("14:05", new NumberFormat("hh:mm").Format(value, CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeParts.Hour | DateTimeParts.Minute, Parts("hh:mm"));
        }
    }
}
