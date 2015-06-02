#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;

namespace Aspectacular.Test
{
    public class SomeTestClass
    {
        private readonly DateTime dt = DateTime.UtcNow;

        public SomeTestClass()
        {
        }

        public SomeTestClass(DateTime dt)
        {
            this.dt = dt;
        }

        public string GetDateString(string prefix)
        {
            return string.Format("{0} {1}", (prefix ?? "[No Prefix]"), this.dt);
        }

        public bool ThrowFailure()
        {
            throw new Exception("Expected exception in a test method.");
        }

        public int DoNothing(int arg = 0, string hkwStr = null, bool bogusBool = true, decimal bogusDec = 0m, IEnumerable<int> sequence = null)
        {
            return arg;
        }

        public static int DoNothingStatic(int arg = 0, string hkwStr = null, bool bogusBool = true, decimal bogusDec = 0m, IEnumerable<int> sequence = null)
        {
            return arg;
        }

        public void DoNothing(string bogus)
        {
// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
// ReSharper disable once RedundantToStringCall
// ReSharper disable once SpecifyACultureInStringConversionExplicitly
            bogus.ToString();
        }

        [InvariantReturn] // <-testing conflict between stated cacheability and output params.
        public static void MiscParmsStatic(int intParm, SomeTestClass classParm, ref string refString, out bool outBool)
        {
            Proxy.CurrentLog.LogInformationData("Static method logging test", DateTime.UtcNow);

            refString = string.Format("{0} {1}", intParm, refString);
            outBool = true;
        }

        [InvariantReturn]
        [RequiredAspect(typeof(DebugOutputAspect), WhenRequiredAspectIsMissing.InstantiateAndAddFirst /*, EntryType.Error | EntryType.Warning | EntryType.Info*/)]
        public bool FakeLogin(string username, [SecretParamValue] string password)
        {
            return (DateTime.UtcNow.Ticks & 1) == 1;
        }
    }

    public class SomeTestDisposable : IDisposable
    {
        public bool? IsOpened { get; set; }

        public SomeTestDisposable()
        {
        }

        public SomeTestDisposable(bool isOpened)
        {
            this.IsOpened = isOpened;
        }

        private void Open()
        {
            if(!this.IsOpened.HasValue || !this.IsOpened.Value)
                this.IsOpened = true;
        }

        private void Close()
        {
            if(this.IsOpened.HasValue && this.IsOpened.Value)
                this.IsOpened = false;
        }

        public void Dispose()
        {
            this.Close();
        }

        public string Echo(string input)
        {
            this.Open();
            return input;
        }
    }
}