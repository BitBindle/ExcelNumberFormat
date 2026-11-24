using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// Excel accepts the color names Black, Blue, Cyan, Green, Magenta, Red, White and Yellow,
// and Color1 to Color56, in any letter case. A color does not change the text of the value.
// https://support.microsoft.com/en-us/excel/review-guidelines-for-customizing-a-number-format

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class ColorTests
    {
        private static readonly string[] ColorNames = { "Black", "Blue", "Cyan", "Green", "Magenta", "Red", "White", "Yellow" };

        string Format(object value, string formatString)
        {
            var format = new NumberFormat(formatString);
            Assert.IsTrue(format.IsValid, formatString);
            return format.Format(value, CultureInfo.InvariantCulture);
        }

        void TestNumberFormat(string formatString, object value, string expected)
        {
            var format = new NumberFormat(formatString);
            Assert.IsTrue(format.IsValid, formatString);
            Assert.AreEqual(expected, format.Format(value, CultureInfo.InvariantCulture), formatString);
            Assert.IsFalse(format.IsDateTimeFormat, formatString);
            Assert.IsFalse(format.IsTimeSpanFormat, formatString);
        }

        [TestMethod]
        public void TestColorNameInAnyCaseFormatsNumber()
        {
            foreach (var name in ColorNames)
            {
                TestNumberFormat("[" + name + "]0", 5, "5");
                TestNumberFormat("[" + name.ToLowerInvariant() + "]0", 5, "5");
                TestNumberFormat("[" + name.ToUpperInvariant() + "]0.00", 5, "5.00");
            }
        }

        [TestMethod]
        public void TestNumberedColorFormatsNumber()
        {
            for (var i = 1; i <= 56; i++)
            {
                TestNumberFormat("[Color" + i + "]0", 5, "5");
                TestNumberFormat("[color" + i + "]0", 5, "5");
            }
        }

        [TestMethod]
        public void TestMagentaInSections()
        {
            TestNumberFormat("[Red]0;[Magenta]-0", -1, "-1");
            TestNumberFormat("[Color3]0.0;[Magenta]-0.0;[Blue]0.0", -1, "-1.0");
            TestNumberFormat("[Magenta]#,##0.00", 1234.5, "1,234.50");
            Assert.AreEqual("Ann", Format("Ann", "0;0;0;[Magenta]@"));
        }

        [TestMethod]
        public void TestMagentaWithElapsedTime()
        {
            var format = new NumberFormat("[Magenta][h]:mm");
            Assert.IsTrue(format.IsTimeSpanFormat);
            Assert.AreEqual("36:00", format.Format(1.5, CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void TestElapsedTimeCodesInAnyCase()
        {
            // 0.5 days is 12 hours, 720 minutes or 43200 seconds
            var cases = new[]
            {
                new[] { "[h]", "12" },
                new[] { "[H]", "12" },
                new[] { "[hh]", "12" },
                new[] { "[m]", "720" },
                new[] { "[M]", "720" },
                new[] { "[mm]", "720" },
                new[] { "[mM]", "720" },
                new[] { "[s]", "43200" },
                new[] { "[S]", "43200" },
                new[] { "[ss]", "43200" },
                new[] { "[h]:mm:ss", "12:00:00" },
                new[] { "[mm]:ss", "720:00" },
            };

            foreach (var item in cases)
            {
                var format = new NumberFormat(item[0]);
                Assert.IsTrue(format.IsValid, item[0]);
                Assert.IsTrue(format.IsTimeSpanFormat, item[0]);
                Assert.AreEqual(item[1], format.Format(0.5, CultureInfo.InvariantCulture), item[0]);
            }
        }
    }
}
