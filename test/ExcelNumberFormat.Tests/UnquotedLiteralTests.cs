using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// Excel shows these characters without quotation marks: $ + ( : ^ ' { < = - / ) ! & ~ } > and space.
// https://support.microsoft.com/en-us/excel/review-guidelines-for-customizing-a-number-format

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class UnquotedLiteralTests
    {
        // The "/" is not in this list, because it makes a fraction in a number section.
        private static readonly string[] Literals = { "$", "+", "(", ":", "^", "'", "{", "<", "=", "-", ")", "!", "&", "~", "}", ">", " ", "°", "¥" };

        void Test(object value, string format, string expected)
        {
            var numberFormat = new NumberFormat(format);
            Assert.AreEqual(expected, numberFormat.Format(value, CultureInfo.InvariantCulture), format);
            Assert.IsTrue(numberFormat.IsValid, format);
        }

        [TestMethod]
        public void TestLiteralInNumberSection()
        {
            foreach (var literal in Literals)
            {
                Test(5, "0" + literal, "5" + literal);
                Test(5, literal + "0", literal + "5");
                Test(5, "0.0" + literal, "5.0" + literal);
            }
        }

        [TestMethod]
        public void TestLiteralInTextSection()
        {
            foreach (var literal in Literals)
                Test("Ann", literal + "@", literal + "Ann");

            Test("Ann", "/@", "/Ann");
        }

        [TestMethod]
        public void TestLiteralInDateSection()
        {
            foreach (var literal in Literals)
                Test(45000, "yyyy" + literal + "mm", "2023" + literal + "03");
        }

        [TestMethod]
        public void TestDegreesMinutesSeconds()
        {
            Test(123456, "##0° 00' 00''", "12° 34' 56''");
        }

        [TestMethod]
        public void TestYenSign()
        {
            Test(12.3, "\"before \"¥#.00", "before ¥12.30");
            Test(-12.3, "\"before \"¥#.00", "-before ¥12.30");
            Test(-12.3, "¥#.00\" after\"", "-¥12.30 after");
        }

        [TestMethod]
        public void TestConditionWithLiterals()
        {
            // <, = and > in brackets are still a condition
            Test(50, "[<=100]\"low\";\"high\"", "low");
            Test(500, "[<=100]\"low\";\"high\"", "high");
            Test(1, "[=1]\"one\";0", "one");
            Test(50, "[<=100]0<;0>", "50<");
            Test(500, "[<=100]0<;0>", "500>");
            Test(150, "[>=100]0=;0~", "150=");
        }

        [TestMethod]
        public void TestLongSystemTimeKeepsSectionWhenPatternHasQuotes()
        {
            // The ' marks quote text in a .NET pattern, so the formatter keeps the codes of the section
            var currentCulture = CultureInfo.CurrentCulture;
            try
            {
                var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                culture.DateTimeFormat.LongTimePattern = "HH'h'mm";
                CultureInfo.CurrentCulture = culture;
                Test(0.5, "[$-F400][h]:mm:ss", "12:00:00");
            }
            finally
            {
                CultureInfo.CurrentCulture = currentCulture;
            }
        }
    }
}
