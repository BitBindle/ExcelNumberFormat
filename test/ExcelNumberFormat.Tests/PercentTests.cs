using System;
using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// Each percent sign multiplies the value by 100. The expected values of TestPercentSignMultipliesBy100 and the first two checks
// of TestEachPercentSignMultipliesBy100 are the cached results of =TEXT(value, format) that Excel 16 saved in the Apache POI
// test file NumberFormatTests.xlsx, sheet Tests, rows 99 to 105:
// https://github.com/apache/poi/blob/trunk/test-data/spreadsheet/NumberFormatTests.xlsx

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class PercentTests
    {
        string Format(object value, string formatString)
        {
            var format = new NumberFormat(formatString);
            if (format.IsValid)
                return format.Format(value, CultureInfo.InvariantCulture);

            return null;
        }

        [TestMethod]
        public void TestPercentSignMultipliesBy100()
        {
            Assert.AreEqual("1.23%", Format(0.0123, "0.00%"));
            Assert.AreEqual("12,345.00%", Format(123.45, "0,0.00%"));
            Assert.AreEqual("-1.23%", Format(-0.0123, "0.00%"));
            Assert.AreEqual("-12,345.00%", Format(-123.45, "0,0.00%"));
            Assert.AreEqual("0.04%", Format(0.00035, "#,##0.00%"));
        }

        [TestMethod]
        public void TestEachPercentSignMultipliesBy100()
        {
            Assert.AreEqual("1,234,500.00%%", Format(123.45, "0,0.00%%"));
            Assert.AreEqual("-1,234,500.00%%", Format(-123.45, "0,0.00%%"));
            Assert.AreEqual("2500%%", Format(0.25, "0%%"));
            Assert.AreEqual("%2500%", Format(0.25, "%0%"));
        }

        [TestMethod]
        public void TestPercentSignsOfEachSection()
        {
            Assert.AreEqual("2500%%", Format(0.25, "0%%;-0%"));
            Assert.AreEqual("-25%", Format(-0.25, "0%%;-0%"));
            Assert.AreEqual("-5", Format(-5, "0%;-0"));
        }

        [TestMethod]
        public void TestZeroWithManyPercentSigns()
        {
            var signs = new string('%', 160);
            Assert.AreEqual("0" + signs, Format(0, "0" + signs));
        }

        [TestMethod]
        public void TestQuotedOrEscapedPercentSignDoesNotMultiply()
        {
            Assert.AreEqual("50%", Format(50, "0\"%\""));
            Assert.AreEqual("50%", Format(50, "0\\%"));
            Assert.AreEqual("50%%", Format(0.5, "0\"%\"%"));
            Assert.AreEqual("50%%", Format(0.5, "0%\\%"));
        }
    }
}
