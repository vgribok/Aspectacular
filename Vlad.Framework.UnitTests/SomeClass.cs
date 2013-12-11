using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Value.Framework.Aspectacular;

namespace Value.Framework.UnitTests
{
    public class SomeTestClass
    {
        public DateTime dt = DateTime.UtcNow;

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
            bogus.ToString();
        }

        public static void MiscParmsStatic(int intParm, SomeTestClass classParm, ref string refString, out bool outBool)
        {
            refString = string.Format("{0} {1}", intParm, refString);
            outBool = true;
        }

        [InvariantReturn]
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
            this.IsOpened = IsOpened;
        }

        void Open()
        {
            if (!this.IsOpened.HasValue || !this.IsOpened.Value)
                this.IsOpened = true;
        }

        void Close()
        {
            if (this.IsOpened.HasValue && this.IsOpened.Value)
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
