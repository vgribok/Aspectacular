using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Core
{
    /// <summary>
    /// Represents a string that is never "" or has only white spaces in it.
    /// </summary>
    public class NonEmptyString : IComparable
    {
        public readonly string theString;

        public NonEmptyString(string daString)
        {
            this.theString = daString.IsBlank() ? null : daString;
        }

        public override string ToString()
        {
            return theString;
        }

        public int CompareTo(object obj)
        {
            if (this.theString == null)
                return obj == null ? 0 : -1;

            return this.theString.CompareTo(obj.ToStringEx(null));
        }

        public static implicit operator string(NonEmptyString noBs)
        {
            return noBs == null ? null : noBs.theString;
        }

        public static implicit operator NonEmptyString(string str)
        {
            return new NonEmptyString(str);
        }
    }
}
