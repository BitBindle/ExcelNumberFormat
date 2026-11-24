using System;
using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// Excel treats 1900 as a leap year, so serial 60 is 29 February 1900 and the weekdays before 1 March 1900
// are one day before the real weekdays: serial 1 is a Sunday.
// https://learn.microsoft.com/en-us/troubleshoot/microsoft-365-apps/excel/wrongly-assumes-1900-is-leap-year

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class Weekday1900Tests
    {
        string Format(object value, string formatString, CultureInfo culture, bool isDate1904 = false)
        {
            var format = new NumberFormat(formatString);
            if (format.IsValid)
                return format.Format(value, culture, isDate1904);

            return null;
        }

        void Test(object value, string format, string expected, bool isDate1904 = false)
        {
            var result = Format(value, format, CultureInfo.InvariantCulture, isDate1904);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestWeekdayBeforeFictitiousLeapDay()
        {
            Test(0, "dddd yyyy-mm-dd", "Saturday 1900-01-00");
            Test(1, "dddd yyyy-mm-dd", "Sunday 1900-01-01");
            Test(2, "dddd yyyy-mm-dd", "Monday 1900-01-02");
            Test(7, "dddd yyyy-mm-dd", "Saturday 1900-01-07");
            Test(31, "dddd yyyy-mm-dd", "Tuesday 1900-01-31");
            Test(59, "dddd yyyy-mm-dd", "Tuesday 1900-02-28");
        }

        [TestMethod]
        public void TestWeekdayFromFictitiousLeapDay()
        {
            Test(60, "dddd yyyy-mm-dd", "Wednesday 1900-02-29");
            Test(61, "dddd yyyy-mm-dd", "Thursday 1900-03-01");
            Test(62, "dddd yyyy-mm-dd", "Friday 1900-03-02");
        }

        [TestMethod]
        public void TestAbbreviatedWeekdayBeforeFictitiousLeapDay()
        {
            Test(0, "ddd d mmm", "Sat 0 Jan");
            Test(1, "ddd d mmm", "Sun 1 Jan");
            Test(59, "ddd d mmm", "Tue 28 Feb");
            Test(60, "ddd d mmm", "Wed 29 Feb");
            Test(61, "ddd d mmm", "Thu 1 Mar");
        }

        [TestMethod]
        public void TestWeekdayOfSerialWithTime()
        {
            Test(1.75, "dddd h:mm", "Sunday 18:00");
            Test(0d, "dddd", "Saturday");
            Test((short)1, "dddd", "Sunday");
            Test(59.5, "dddd d", "Tuesday 28");
        }

        [TestMethod]
        public void TestWeekdayOfCultureBeforeFictitiousLeapDay()
        {
            var de = new CultureInfo("de-DE");
            Assert.AreEqual("Sonntag, 1. Januar 1900", Format(1, "dddd, d. mmmm yyyy", de));
            Assert.AreEqual("So", Format(1, "ddd", de));
        }

        [TestMethod]
        public void TestSystemLongDateBeforeFictitiousLeapDay()
        {
            // [$-F800] shows the long date pattern of the culture
            var en = new CultureInfo("en-US");
            const string code = "[$-F800]dddd, mmmm dd, yyyy";
            Assert.AreEqual("Saturday, January 0, 1900", Format(0, code, en));
            Assert.AreEqual("Sunday, January 1, 1900", Format(1, code, en));
            Assert.AreEqual("Tuesday, February 28, 1900", Format(59, code, en));
            Assert.AreEqual("Wednesday, February 29, 1900", Format(60, code, en));
            Assert.AreEqual("Thursday, March 1, 1900", Format(61, code, en));
            Assert.AreEqual("Samstag, 0. Januar 1900", Format(0, code, new CultureInfo("de-DE")));
        }

        [TestMethod]
        public void TestSystemLongDateOfCultures()
        {
            // 1928 is a leap year that starts on a Sunday, so its days have the weekdays of the Excel days in 1900.
            // The cultures have quoted literals, genitive month names and other field orders in their long date patterns.
            foreach (var name in new[] { "en-US", "de-DE", "da-DK", "es-ES", "eu-ES", "hu-HU", "ja-JP", "ko-KR", "lt-LT", "pl-PL", "ru-RU" })
            {
                var culture = new CultureInfo(name);
                var pattern = culture.DateTimeFormat.LongDatePattern;
                foreach (var serial in new[] { 1, 2, 31, 32, 59, 60 })
                {
                    var expected = new DateTime(1928, 1, 1).AddDays(serial - 1).ToString(pattern, culture).Replace("1928", "1900");
                    Assert.AreEqual(expected, Format(serial, "[$-F800]dddd, mmmm dd, yyyy", culture), culture.Name + " " + serial);
                }
            }
        }

        [TestMethod]
        public void TestSystemLongDateOfOtherCalendarIsNotChanged()
        {
            // The fields of another calendar are not the fields of the Excel date
            var th = new CultureInfo("th-TH");
            Assert.AreEqual(new DateTime(1900, 1, 1).ToString(th.DateTimeFormat.LongDatePattern, th), Format(1, "[$-F800]dddd, mmmm dd, yyyy", th));
        }

        [TestMethod]
        public void TestWeekday1904IsNotChanged()
        {
            // In the 1904 date system, serial 0 is Friday 1 January 1904
            Test(0, "dddd yyyy-mm-dd", "Friday 1904-01-01", true);
            Test(1, "dddd yyyy-mm-dd", "Saturday 1904-01-02", true);
            Test(59, "dddd yyyy-mm-dd", "Monday 1904-02-29", true);
            Test(60, "dddd yyyy-mm-dd", "Tuesday 1904-03-01", true);
        }

        [TestMethod]
        public void TestWeekdayOfDateTimeIsNotChanged()
        {
            // A DateTime holds a real date, so it keeps the real weekday
            Test(new DateTime(1900, 1, 1), "dddd yyyy-mm-dd", "Monday 1900-01-01");
            Test(new DateTime(1900, 2, 28), "dddd yyyy-mm-dd", "Wednesday 1900-02-28");
        }
    }
}
