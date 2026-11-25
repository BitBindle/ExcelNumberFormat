using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class ConditionSignTests
    {
        void Test(double value, string format, string expected)
        {
            var numberFormat = new NumberFormat(format);
            Assert.IsTrue(numberFormat.IsValid, format);
            Assert.AreEqual(expected, numberFormat.Format(value, CultureInfo.InvariantCulture), format);
        }

        // The cached results of the TEXT formulas that Excel saved in the Apache POI test file
        // FormatConditionTests.xlsx, sheet 2 (rows 24, 25, 29 to 32, 34, 35 and 37).
        // https://github.com/apache/poi/blob/trunk/test-data/spreadsheet/FormatConditionTests.xlsx
        [TestMethod]
        public void TestConditionThatPositiveValuesCanMeetKeepsSign()
        {
            Test(-9, "[>-10]#\" Big\"", "-9 Big");
            Test(-8, "[>-10]#\" Big\"", "-8 Big");
            Test(-10, "[>=-10]#\" Big\"", "-10 Big");
            Test(-9, "[>=-10]#\" Big\"", "-9 Big");
            Test(-8, "[>=-10]#\" Big\"", "-8 Big");
            Test(-11, "[<>-10]#\" Wow\"", "-11 Wow");
            Test(-9, "[<>-10]#\" Wow\"", "-9 Wow");
            Test(-11, "[=-10]#\" Wow\"", "-11");
            Test(-9, "[=-10]#\" Wow\"", "-9");
        }

        // Excel hides the minus sign only when no positive value meets the condition of the section
        // (andersnm/ExcelNumberFormat#55: "[<2]0" shows -1 as "-1"). In FormatConditionTests.xlsx, the rows of the
        // conditions that hide the sign are typed values, not Excel results, so these expected values come from #55.
        [TestMethod]
        public void TestSignDependsOnWhetherPositiveValuesCanMeetCondition()
        {
            Test(-1, "[<2]0", "-1");
            Test(-1, "[<=100]0;0", "-1");
            Test(-1, "[Red][<=100]0;[Blue]0", "-1");
            Test(-1, "[<>0]0;0", "-1");
            Test(-11, "[<-10]#\" Wow\"", "11 Wow");
            Test(-10, "[<=-10]#\" Wow\"", "10 Wow");
            Test(-10, "[=-10]#\" Wow\"", "10 Wow");
            Test(-5, "[<0]0", "5");
            Test(-5, "[<=0]0;0", "5");
            Test(-150, "[>100]0;[<-100]0;0", "150");
            Test(-5, "[>100]0;[>-50]0;0", "-5");
            Test(-5, "[>100]0;[>50]0;[<0]0", "-5");
        }
    }
}
