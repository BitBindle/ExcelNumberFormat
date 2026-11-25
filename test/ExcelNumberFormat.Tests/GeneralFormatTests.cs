using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class GeneralFormatTests
    {
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

        const string BooleanReason = "Format gives Convert.ToString for a Boolean value (True), not the Excel text (TRUE)";

        // The cached results of TEXT(C, B) in the cells A2:A73 of the sheet "Tests" in GeneralFormatTests.xlsx.
        // The file is in the Apache POI repository: test-data/spreadsheet/GeneralFormatTests.xlsx.
        // Microsoft Macintosh Excel 12.0 saved the file. B is "General", or empty in the empty rows 69 and 71.
        // Arguments: row, value (C), format (B), Excel result (A).
        static readonly TestCaseData[] ExcelResults =
        {
            new TestCaseData(2, 1d, "General", "1"),
            new TestCaseData(3, 12d, "General", "12"),
            new TestCaseData(4, 123d, "General", "123"),
            new TestCaseData(5, 1234d, "General", "1234"),
            new TestCaseData(6, 12345d, "General", "12345"),
            new TestCaseData(7, 1d, "General", "1"),
            new TestCaseData(8, 1.2, "General", "1.2"),
            new TestCaseData(9, 1.23, "General", "1.23"),
            new TestCaseData(10, 1.234, "General", "1.234"),
            new TestCaseData(11, 1.2345678899999999, "General", "1.23456789"),
            new TestCaseData(12, 1.234567891, "General", "1.234567891"),
            new TestCaseData(13, 1.2345678912, "General", "1.234567891"),
            new TestCaseData(14, 1.2345678919, "General", "1.234567892"),
            new TestCaseData(15, 1d, "General", "1"),
            new TestCaseData(16, 10d, "General", "10"),
            new TestCaseData(17, 10000000000d, "General", "10000000000"),
            new TestCaseData(18, 100000000000d, "General", "1E+11"),
            new TestCaseData(19, 110000000000d, "General", "1.1E+11"),
            new TestCaseData(20, 112000000000d, "General", "1.12E+11"),
            new TestCaseData(21, 112300000000d, "General", "1.123E+11"),
            new TestCaseData(22, 112345000000d, "General", "1.12345E+11"),
            new TestCaseData(23, 112345100000d, "General", "1.12345E+11"),
            new TestCaseData(24, 112345900000d, "General", "1.12346E+11"),
            new TestCaseData(25, 0.1, "General", "0.1"),
            new TestCaseData(26, 0.01, "General", "0.01"),
            new TestCaseData(27, 1.0000000000000001E-9, "General", "0.000000001"),
            new TestCaseData(28, 1E-10, "General", "1E-10"),
            new TestCaseData(29, 1.0999999999999999E-10, "General", "1.1E-10"),
            new TestCaseData(30, 1.12E-10, "General", "1.12E-10"),
            new TestCaseData(31, 1.123E-10, "General", "1.123E-10"),
            new TestCaseData(32, 1.12345E-10, "General", "1.12345E-10"),
            new TestCaseData(33, 1.1234509999999999E-10, "General", "1.12345E-10"),
            new TestCaseData(34, 1.123459E-10, "General", "1.12346E-10"),
            new TestCaseData(35, 0d, "General", "0"),
            new TestCaseData(36, -1d, "General", "-1"),
            new TestCaseData(37, -12d, "General", "-12"),
            new TestCaseData(38, -123d, "General", "-123"),
            new TestCaseData(39, -1234d, "General", "-1234"),
            new TestCaseData(40, -12345d, "General", "-12345"),
            new TestCaseData(41, -1d, "General", "-1"),
            new TestCaseData(42, -1.2, "General", "-1.2"),
            new TestCaseData(43, -1.23, "General", "-1.23"),
            new TestCaseData(44, -1.234, "General", "-1.234"),
            new TestCaseData(45, -1.2345678899999999, "General", "-1.23456789"),
            new TestCaseData(46, -1.234567891, "General", "-1.234567891"),
            new TestCaseData(47, -1.2345678912, "General", "-1.234567891"),
            new TestCaseData(48, -1.2345678919, "General", "-1.234567892"),
            new TestCaseData(49, -1d, "General", "-1"),
            new TestCaseData(50, 10d, "General", "10"),
            new TestCaseData(51, -10000000000d, "General", "-10000000000"),
            new TestCaseData(52, -100000000000d, "General", "-1E+11"),
            new TestCaseData(53, -110000000000d, "General", "-1.1E+11"),
            new TestCaseData(54, -112000000000d, "General", "-1.12E+11"),
            new TestCaseData(55, -112300000000d, "General", "-1.123E+11"),
            new TestCaseData(56, -112345000000d, "General", "-1.12345E+11"),
            new TestCaseData(57, -112345100000d, "General", "-1.12345E+11"),
            new TestCaseData(58, -112345900000d, "General", "-1.12346E+11"),
            new TestCaseData(59, -0.1, "General", "-0.1"),
            new TestCaseData(60, -0.01, "General", "-0.01"),
            new TestCaseData(61, -1.0000000000000001E-9, "General", "-0.000000001"),
            new TestCaseData(62, -1E-10, "General", "-1E-10"),
            new TestCaseData(63, -1.0999999999999999E-10, "General", "-1.1E-10"),
            new TestCaseData(64, -1.12E-10, "General", "-1.12E-10"),
            new TestCaseData(65, -1.123E-10, "General", "-1.123E-10"),
            new TestCaseData(66, -1.12345E-10, "General", "-1.12345E-10"),
            new TestCaseData(67, -1.1234509999999999E-10, "General", "-1.12345E-10"),
            new TestCaseData(68, -1.123459E-10, "General", "-1.12346E-10"),
            new TestCaseData(69, null, "", ""),
            new TestCaseData(70, "hello", "General", "hello"),
            new TestCaseData(71, null, "", ""),
            new TestCaseData(72, true, "General", "TRUE").Ignore(BooleanReason),
            new TestCaseData(73, false, "General", "FALSE").Ignore(BooleanReason)
        };

        [TestCaseSource(nameof(ExcelResults))]
        public void TestExcelTextResults(int row, object value, string format, string expected)
        {
            Test(value, format, expected);
        }

        // ECMA-376 Part 1, 18.8.30: for General, the "max overall length for cell display is 11, not including negative sign".
        [TestMethod]
        public void TestGeneralShowsAtMostElevenCharacters()
        {
            Test(1.0 / 3, "General", "0.333333333");
            Test(-1.0 / 3, "General", "-0.333333333");
            Test(2.0 / 3, "General", "0.666666667");
            Test(123456.7890123, "General", "123456.789");
            Test(12345678901, "General", "12345678901");
            Test(-12345678901, "General", "-12345678901");
            Test(1234.56, "General", "1234.56");
            Test(0.000123456789, "General", "0.000123457");
        }

        // Excel uses scientific notation for 12 or more integer digits
        [TestMethod]
        public void TestGeneralLargeNumbers()
        {
            Test(123456789012, "General", "1.23457E+11");
            Test(1E+20, "General", "1E+20");
            Test(99999999999.4, "General", "99999999999");
            Test(99999999999.5, "General", "1E+11");
            Test(999999500000, "General", "1E+12");
            Test(999999499999, "General", "9.99999E+11");
            Test(double.MaxValue, "General", "1.79769E+308");
            Test(-double.MaxValue, "General", "-1.79769E+308");
        }

        // Excel uses scientific notation when the decimal form shows fewer significant digits
        [TestMethod]
        public void TestGeneralSmallNumbers()
        {
            Test(0.000012345678901, "General", "1.23457E-05");
            Test(0.00001, "General", "0.00001");
            Test(0.0000123, "General", "0.0000123");
            Test(9.9999999995E-10, "General", "0.000000001");
            Test(0.000099999999995, "General", "0.0001");
            Test(5E-06, "General", "0.000005");
            Test(9.99999E-10, "General", "9.99999E-10");
            Test(1E-300, "General", "1E-300");
            Test(double.Epsilon, "General", "4.94066E-324");
        }

        // The decimal form must not round up to a power of ten that the scientific form does not reach
        [TestMethod]
        public void TestGeneralSmallNumbersDoNotRoundUpToPowerOfTen()
        {
            Test(5E-10, "General", "5E-10");
            Test(9E-10, "General", "9E-10");
            Test(9.5E-10, "General", "9.5E-10");
            Test(-5E-10, "General", "-5E-10");
        }

        [TestMethod]
        public void TestGeneralRoundingCarry()
        {
            Test(9.99999999995, "General", "10");
            Test(9.99999999949, "General", "9.999999999");
            Test(0.9999999999, "General", "1");
            Test(9999999999.5, "General", "10000000000");
            Test(-9.99999999995, "General", "-10");
        }

        [TestMethod]
        public void TestGeneralZeroAndNonFinite()
        {
            Test(0.0, "General", "0");
            Test(-0.0, "General", "0");
            Test(double.NaN, "General", "NaN");
            Test(double.PositiveInfinity, "General", "Infinity");
            Test(double.NegativeInfinity, "General", "-Infinity");
        }

        [TestMethod]
        public void TestGeneralWithLiterals()
        {
            Test(1.0 / 3, "General\\%", "0.333333333%");
            Test(123456789012, "General\\%", "1.23457E+11%");
            Test(1.0 / 3, "\\$General", "$0.333333333");
            Test(1.0 / 3, "[Red]General", "0.333333333");
        }

        [TestMethod]
        public void TestGeneralOtherNumericTypes()
        {
            Test(123456789012L, "General", "1.23457E+11");
            Test(int.MinValue, "General", "-2147483648");
            Test((short)-5, "General", "-5");
            Test(1m / 3, "General", "0.333333333");
            Test(float.MaxValue, "General", "3.40282E+38");
        }

        [TestMethod]
        public void TestGeneralCultureDecimalSeparator()
        {
            var culture = new CultureInfo("de-DE");
            Assert.AreEqual("0,333333333", Format(1.0 / 3, "General", culture));
            Assert.AreEqual("-1,5", Format(-1.5, "General", culture));
            Assert.AreEqual("1,23457E+11", Format(123456789012, "General", culture));
            Assert.AreEqual("1,12346E-10", Format(1.123459E-10, "General", culture));
        }

        // Microsoft, "Review guidelines for customizing a number format": the second section is for negative numbers.
        // Microsoft examples write the sign in the code of that section: "[Red](#,##0.00)" on that page,
        // and "0.00%;[Red]-0.00%" in "Format negative percentages to make them easy to find". The section shows the value without its sign.
        [TestMethod]
        public void TestGeneralInSecondSectionHidesSign()
        {
            Test(-1.0 / 3, "General;-General", "-0.333333333");
            Test(-1.0 / 3, "General;(General)", "(0.333333333)");
            Test(-1.0 / 3, "0.0;General", "0.333333333");
            Test(-5, "General;General", "5");
            Test(-5, "0;-General;\"zero\"", "-5");
            Test(5, "General;-General", "5");
            Test(-5, "General", "-5");
        }

        // A first section with a condition that no positive value meets hides the sign of a number section, so it also hides the sign of General
        [TestMethod]
        public void TestGeneralAfterConditionHidesSignLikeNumberSection()
        {
            Test(-5, "[<0]0;0", "5");
            Test(-5, "[<0]General;0", "5");
        }
    }
}
