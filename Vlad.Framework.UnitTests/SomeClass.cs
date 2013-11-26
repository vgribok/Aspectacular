using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
