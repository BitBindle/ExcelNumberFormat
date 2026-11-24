using System;
using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// Excel treats 1900 as a leap year, so serial 60 is 29 February 1900 and the weekdays before 1 March 1900
// are one day before the real weekdays: serial 1 is a Sunday.
// https://learn.microsoft.com/office/troubleshoot/excel/wrongly-assumes-1900-is-leap-year

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
