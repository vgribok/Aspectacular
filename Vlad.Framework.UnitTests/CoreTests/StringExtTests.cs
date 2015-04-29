#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.CoreTests
{
    [TestClass]
    public class StringExtTests
    {
        [TestMethod]
        public void TestNullToString()
        {
            string nullStr = null;
            string actual = nullStr.ToStringEx("hahaha!");
            Assert.AreEqual("hahaha!", actual);

            actual = "Привет, Вася!".ToLowerEx();
            Assert.AreEqual("привет, вася!", actual);

            actual = "Привет, Вася!".ToUpperInvariantEx();
            Assert.AreEqual("ПРИВЕТ, ВАСЯ!", actual);

            actual = "Fälla".ToLowerEx();
            Assert.AreEqual("fälla", actual);

            actual = "Fälla".ToLowerInvariantEx();
            Assert.AreEqual("fälla", actual);

            actual = nullStr.ToLowerEx();
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void TestNonEmptyString()
        {
            NonEmptyString nbs = null;
            Assert.IsNull(nbs);
            Assert.IsTrue(nbs == null);
            Assert.AreEqual("Empty", nbs.ToStringEx("Empty"));
            Assert.IsNull(nbs.ToStringEx(null));

            nbs = string.Empty;
            Assert.IsNotNull(nbs);
            Assert.IsTrue(nbs == null);

            nbs = "Hello!";
            Assert.IsTrue(nbs == "Hello!");
            Assert.AreEqual(nbs, "Hello!");
            Assert.IsTrue(nbs.CompareTo("abc") > 0);

            NonEmptyTrimmedString ts = "";
            Assert.IsNotNull(ts);
            Assert.IsTrue(ts == null);

            ts = " Something ";
            Assert.AreEqual(ts, "Something");
            Assert.IsTrue("Something" == ts);
        }

        public class TestTruncatedString : TruncatedString
        {
            public TestTruncatedString(string str, uint maxLength, string optionalEllipsis = "...")
                : base(str, maxLength, optionalEllipsis)
            {
            }
        }

        [TestMethod]
        public void TruncatedStringTest()
        {
            TestTruncatedString ts = new TestTruncatedString("Short string", 10000, "doesn't matter");
            Assert.AreEqual(ts, "Short string");

            ts = new TestTruncatedString("Short string", 5, "........very long ellipsis......");
            Assert.AreEqual(ts, "Short");

            ts = new TestTruncatedString("Whatever", 7);
            Assert.AreEqual(ts, "What...");

            ts = new TestTruncatedString("Very long string", 4, null);
            Assert.AreEqual(ts, "Very");

            ts = new TestTruncatedString("Very long string", 10);
            Assert.AreEqual(ts, "Very lo...");

            String255 s255 =
                "By eliminating unnecessary casts, implicit conversions can improve source code readability. However, because implicit conversions do not require programmers to explicitly cast from one type to the other, care must be taken to prevent unexpected results. In general, implicit conversion operators should never throw exceptions and never lose information so that they can be used safely without the programmer's awareness. If a conversion operator cannot meet those criteria, it should be marked explicit. For more information, see Using Conversion Operators.";
            string actual = s255;
            Assert.AreEqual(255, actual.Length);
        }

        [TestMethod]
        public void TestParsing()
        {
            string str = null;
            bool actual = str.Parse(false);
            Assert.AreEqual(false, actual);

            actual = "True".Parse(false);
            Assert.AreEqual(true, actual);
        }

        public class TestEntity
        {
            public string Text { get; set; }
            public int Count { get; set; }
            public NonEmptyTrimmedString FancyString { get; set; }
            public DateTime Date { get; set; }
            //public ValueRange<int> IntRange { get; set; }
            public Range<string> StringRange { get; set; }
        }

        [TestMethod]
        public void TestConstrainedStringSerialization()
        {
            TestEntity te = new TestEntity
            {
                Count = 23,
                Date = DateTime.Now,
                Text = " Any Text ",
                FancyString = " Fancy Text ",
                //IntRange = new ValueRange<int>(-3, 5),
                StringRange = new Range<string>(null, "xyz"),
            };

            string json = te.ToJsonString();
            TestEntity te2 = json.FromJsonString<TestEntity>();
            Assert.AreEqual(te.FancyString, te2.FancyString);
            //Assert.AreEqual(te.IntRange, te2.IntRange);
            Assert.AreEqual(te.StringRange, te2.StringRange);
        }

        [TestMethod]
        public void TestStringLeftAndRight()
        {
            string actual = "@".RightOf("@");
            Assert.AreEqual("", actual);

            actual = "12@34".RightOf("@");
            Assert.AreEqual("34", actual);

            actual = (null as string).RightOf("@");
            Assert.AreEqual(null, actual);

            actual = "@".RightOf("+");
            Assert.AreEqual(null, actual);

            actual = "@".LeftOf("@");
            Assert.AreEqual("", actual);

            actual = "12@34".LeftOf("@");
            Assert.AreEqual("12", actual);

            actual = "@".LeftOf("+");
            Assert.AreEqual(null, actual);
        }


        [TestMethod]
        public void ConfigurationManagerParsingTest()
        {
            const int expectedNumber = 3600000;
            int actual = ConfigurationManager.AppSettings["TotallyBogus"].Parse(defaultValue: expectedNumber);
            Assert.AreEqual(expectedNumber, actual);
        }
    }
}