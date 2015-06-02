#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet_UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string str = "Hello, World!";

            //int index = str.GetProxy().Invoke(s => s.IndexOf("lo")); // Don't forget to uncomment default aspect parts in the app.config.
            //Assert.AreEqual(3, index);
        }
    }
}