using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    /// <summary>
    /// Represents a string that is never "" or has only white spaces in it: it's either null or non-empty, non-whitespace string.
    /// </summary>
    public class NonEmptyString : IComparable, IComparable<NonEmptyString>, IComparable<string>, IEquatable<NonEmptyString>, IEquatable<string>
    {
        public readonly string theString;

        public NonEmptyString(string daString)
        {
            this.theString = daString.IsBlank() ? null : daString;
        }

        public static bool operator == (NonEmptyString nbs, object obj)
        {
            object nbsRaw = nbs;

            if(obj == null)
                return nbsRaw == null || nbs.theString == null;

            string str = nbsRaw == null ? null : nbs.theString;
            return str == obj.ToStringEx(null);
        }

        public static bool operator !=(NonEmptyString nbs, object obj)
        {
            bool same = nbs == obj;
            return !same;
        }

        public static implicit operator string(NonEmptyString noBs)
        {
            return noBs == null ? null : noBs.theString;
        }

        public static implicit operator NonEmptyString(string str)
        {
            return new NonEmptyString(str);
        }

        #region Overrides and interface implementations

        public int CompareTo(object obj)
        {
            string other = null;

            if (obj != null)
            {
                if (obj is string)
                    other = (string)obj;
                else if (obj is NonEmptyString)
                    other = obj.ToString();
                else
                    throw new Exception("Cannot compare NonEmptyString to \"{0}\".".SmartFormat(obj.GetType().FormatCSharp()));
            }

            if (this.theString == null)
                return other == null ? 0 : int.MinValue;

            return this.theString.CompareTo(other);
        }

        public override string ToString()
        {
            return this.theString;
        }

        public override bool Equals(object obj)
        {
            return this.theString == obj.ToStringEx(null);
        }

        public override int GetHashCode()
        {
            return this.theString == null ? 0 : this.theString.GetHashCode();
        }

        public int CompareTo(NonEmptyString other)
        {
            return this.CompareTo((object)other);
        }

        public int CompareTo(string other)
        {
            return this.CompareTo((object)other);
        }

        public bool Equals(NonEmptyString other)
        {
            return this.Equals(other.ToStringEx(null));
        }

        public bool Equals(string other)
        {
            return this.theString == other;
        }

        #endregion Overrides and interface implementations
    }
}
