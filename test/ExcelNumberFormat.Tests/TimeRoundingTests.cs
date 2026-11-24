using System;
using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// The rule of the library: without fractional second digits, a section that shows a time rounds to whole seconds,
// and the carry goes into the minutes, hours and days. Codes for larger units then show whole units only.
// A seconds field never shows 60: Excel documents ss as "Seconds as 00-59".
// https://support.microsoft.com/en-us/excel/format-numbers-as-dates-or-times
// A section that shows only the date keeps the time of day as it is.

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class TimeRoundingTests
    {
        void Test(object value, string format, string expected)
        {
            var numberFormat = new NumberFormat(format);
            Assert.IsTrue(numberFormat.IsValid, format);
            Assert.AreEqual(expected, numberFormat.Format(value, CultureInfo.InvariantCulture), format);
        }

        static double Seconds(double seconds)
        {
            return seconds / 86400;
        }

        [TestMethod]
        public void TestTimeRoundsToWholeSeconds()
        {
            Test(Seconds(36000.6), "h:mm:ss", "10:00:01");
            Test(Seconds(36000.4), "h:mm:ss", "10:00:00");
            Test(Seconds(59.6), "mm:ss", "01:00");
            Test(Seconds(59.6), "ss", "00");
            Test(0.99999999, "h:mm:ss", "0:00:00");
            Test(0.99999999, "h:mm:ss AM/PM", "12:00:00 AM");
            Test(new DateTime(2024, 7, 25, 2, 57, 59, 600), "hh:mm:ss", "02:58:00");
        }

        [TestMethod]
        public void TestTimeWithoutSecondsRoundsToSecondsThenCutsMinutes()
        {
            // Round to whole seconds, then cut the larger units
            Test(Seconds(9 * 3600 + 36), "h:mm", "9:00");
            Test(Seconds(9 * 3600 + 59.6), "h:mm", "9:01");
        }

        [TestMethod]
        public void TestRoundingCarriesIntoDate()
        {
            Test(44995.99999999, "yyyy-mm-dd hh:mm:ss", "2023-03-11 00:00:00");
            Test(44995.99999999, "d/m/yyyy h:mm", "11/3/2023 0:00");
            Test(new DateTime(2023, 3, 10, 23, 59, 59, 700), "yyyy-mm-dd hh:mm:ss", "2023-03-11 00:00:00");
        }

        [TestMethod]
        public void TestDateOnlyDoesNotRound()
        {
            Test(44995.99999999, "yyyy-mm-dd", "2023-03-10");
            Test(44995.99999999, "dddd", "Friday");
            Test(new DateTime(2023, 3, 10, 23, 59, 59, 600), "yyyy-mm-dd", "2023-03-10");
            Test(new DateTime(2023, 3, 10, 23, 59, 59, 600), "dddd", "Friday");
            Test(2958465.9999999, "yyyy-mm-dd", "9999-12-31");
            Test(44995.99999999, "[$-F800]dddd, mmmm dd, yyyy", "Friday, 10 March 2023");
        }

        [TestMethod]
        public void TestElapsedTimeRoundsWithCarry()
        {
            // Excel shows 59.9 seconds as 0:01:00 in [h]:mm:ss
            Test(Seconds(59.9), "[h]:mm:ss", "0:01:00");
            Test(0.99999999, "[h]:mm:ss", "24:00:00");
            Test(-0.99999999, "[h]:mm:ss", "-24:00:00");
            Test(Seconds(59.6), "[mm]:ss", "01:00");
            Test(Seconds(-59.6), "[mm]:ss", "-01:00");
            Test(Seconds(3659.6), "[h]:mm:ss", "1:01:00");
            Test(new TimeSpan(0, 0, 0, 59, 600), "[mm]:ss", "01:00");
            Test(0.99999999, "[h]", "24");
            Test(0.99999999, "[ss]", "86400");
        }

        [TestMethod]
        public void TestElapsedSecondsAreNotCut()
        {
            // 1.005 days is 86832 seconds. The nearest double is a little less.
            Test(1.005, "[ss]", "86832");
            Test(1.005, "[ss].0", "86832.0");
            Test(1.005, "[h]:mm:ss", "24:07:12");
        }
    }
}
