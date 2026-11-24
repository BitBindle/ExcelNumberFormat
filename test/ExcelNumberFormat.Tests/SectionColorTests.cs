using System;
using System.Globalization;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

// A grid shows a value in the color of the section that formats it, for example negative numbers in red.
// Excel accepts the eight color names and [Color1] to [Color56]:
// https://support.microsoft.com/en-us/excel/review-guidelines-for-customizing-a-number-format

namespace ExcelNumberFormat.Tests
{
    [TestClass]
    public class SectionColorTests
    {
        string Color(string formatString, object value)
        {
            var color = new NumberFormat(formatString).GetColor(value);
            return color == null ? null : color.Name + "/" + color.Index.ToString(CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void TestColorNames()
        {
            // A name has the index of the same color in the default palette
            Assert.AreEqual("Black/1", Color("[Black]0", 1));
            Assert.AreEqual("White/2", Color("[White]0", 1));
            Assert.AreEqual("Red/3", Color("[Red]0", 1));
            Assert.AreEqual("Green/4", Color("[Green]0", 1));
            Assert.AreEqual("Blue/5", Color("[Blue]0", 1));
            Assert.AreEqual("Yellow/6", Color("[Yellow]0", 1));
            Assert.AreEqual("Cyan/8", Color("[Cyan]0", 1));
        }

        [TestMethod]
        public void TestColorNamesIgnoreLetterCase()
        {
            Assert.AreEqual("Red/3", Color("[RED]0", 1));
            Assert.AreEqual("Blue/5", Color("[blue]0", 1));
            Assert.AreEqual("Green/4", Color("[gReEn]0.00", 1));
        }

        [TestMethod]
        public void TestColorIndexes()
        {
            Assert.AreEqual("/1", Color("[Color1]0", 1));
            Assert.AreEqual("/10", Color("[Color10]0", 1));
            Assert.AreEqual("/56", Color("[Color56]0", 1));
            Assert.AreEqual("/3", Color("[COLOR3]0", 1));
            Assert.AreEqual("/45", Color("[color45]0", 1));
        }

        [TestMethod]
        public void TestNoColor()
        {
            Assert.IsNull(Color("0", 1));
            Assert.IsNull(Color("General", 1));
            Assert.IsNull(Color("[Red]0;0", -1));
            Assert.IsNull(Color("[Color0]0", 1));
            Assert.IsNull(Color("[Color57]0", 1));
            Assert.IsNull(Color("[Color]0", 1));
            Assert.IsNull(Color("[Redx]0", 1));
            Assert.IsNull(Color("[Red]0.0.0\"", 1));
            Assert.IsNull(Color("[Red]0", new object()));
            Assert.IsNull(Color("[Red]0", null));
        }

        [TestMethod]
        public void TestColorOfPositiveNegativeAndZeroSections()
        {
            const string format = "[Blue]#,##0;[Red]-#,##0;[Green]0;[Cyan]@";
            Assert.AreEqual("Blue/5", Color(format, 5));
            Assert.AreEqual("Red/3", Color(format, -5));
            Assert.AreEqual("Green/4", Color(format, 0));
            Assert.AreEqual("Cyan/8", Color(format, "text"));
            Assert.AreEqual("Red/3", Color(format, -5.5m));
            Assert.AreEqual("Blue/5", Color(format, 5L));

            Assert.AreEqual("Blue/5", Color("[Blue]0;[Red]-0", 0));
            Assert.AreEqual("Red/3", Color("[Blue]0;[Red]-0", -1));
        }

        [TestMethod]
        public void TestColorOfConditionSections()
        {
            const string format = "[Red][<=100]0;[Blue][>100]0";
            Assert.AreEqual("Red/3", Color(format, 50));
            Assert.AreEqual("Blue/5", Color(format, 150));
            Assert.AreEqual("/10", Color("[>=1000][Color10]#,##0;[Color20]0", 1000));
            Assert.AreEqual("/20", Color("[>=1000][Color10]#,##0;[Color20]0", 999));
        }

        [TestMethod]
        public void TestColorOfTextSection()
        {
            // With fewer than four sections, the section that contains @ formats text
            Assert.AreEqual("Blue/5", Color("[Red]0;[Blue]@", "text"));
            Assert.IsNull(Color("[Red]0;[Blue]-0", "text"));
        }

        [TestMethod]
        public void TestColorOfDateAndDurationSections()
        {
            Assert.AreEqual("Red/3", Color("[Red]yyyy-mm-dd", new DateTime(2023, 3, 10)));
            Assert.AreEqual("Red/3", Color("[Red]yyyy-mm-dd", 44995));
            Assert.AreEqual("Blue/5", Color("[Blue][h]:mm", TimeSpan.FromHours(30)));
        }

        [TestMethod]
        public void TestColorWhenFormatReturnsValueAsText()
        {
            // Format cannot convert a TimeSpan to a number and returns it as text, but the section is the same
            var format = new NumberFormat("[Red]0");
            Assert.AreEqual("01:00:00", format.Format(TimeSpan.FromHours(1), CultureInfo.InvariantCulture));
            Assert.AreEqual("Red/3", Color("[Red]0", TimeSpan.FromHours(1)));
        }

        [TestMethod]
        public void TestColorDoesNotChangeText()
        {
            Assert.AreEqual("1.5", new NumberFormat("[Color10]0.0").Format(1.5, CultureInfo.InvariantCulture));
            Assert.AreEqual("-2", new NumberFormat("[Blue]0;[Color3]-0").Format(-2, CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void TestColorToString()
        {
            Assert.AreEqual("Red", new NumberFormat("[red]0").GetColor(1).ToString());
            Assert.AreEqual("Color10", new NumberFormat("[color10]0").GetColor(1).ToString());
        }
    }
}
