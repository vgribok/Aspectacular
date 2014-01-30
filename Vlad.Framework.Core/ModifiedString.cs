using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aspectacular
{
    /// <summary>
    /// Base class for special types of Strings, with some constraints, like non-null, non-blank, trimmed, etc.
    /// This class is easily convertible to and from System.String, and therefore its subclasses can be used as method parameters instead of some strings.
    /// and can be compared to string and other StringWithConstraints subclasses.
    /// </summary>
    public abstract class StringWithConstraints : IComparable, IComparable<StringWithConstraints>, IComparable<string>, IEquatable<StringWithConstraints>, IEquatable<string>
    {
        public virtual string String { get; protected set;}

        protected StringWithConstraints(string str)
        {
            this.String = str;
        }

        public static bool operator ==(StringWithConstraints ms, object obj)
        {
            object strRaw = ms;

            if (obj == null)
                return strRaw == null || ms.String == null;

            string str = strRaw == null ? null : ms.String;
            return str == obj.ToStringEx(null);
        }

        public static bool operator !=(StringWithConstraints ms, object obj)
        {
            bool same = ms == obj;
            return !same;
        }

        public static implicit operator string(StringWithConstraints ms)
        {
            return ms == null ? null : ms.String;
        }

        //public static implicit operator StringWithConstraints(string str)
        //{
        //    return new StringWithConstraints(str);
        //}

        #region Overrides and interface implementations

        public int CompareTo(object obj)
        {
            string other = null;

            if (obj != null)
            {
                if (obj is string)
                    other = (string)obj;
                else if (obj is StringWithConstraints)
                    other = obj.ToString();
                else
                    throw new Exception("Cannot compare ModifiedString to \"{0}\".".SmartFormat(obj.GetType().FormatCSharp()));
            }

            if (this.String == null)
                return other == null ? 0 : int.MinValue;

            return this.String.CompareTo(other);
        }

        public override string ToString()
        {
            return this.String;
        }

        public override bool Equals(object obj)
        {
            return this.String == obj.ToStringEx(null);
        }

        public override int GetHashCode()
        {
            return this.String == null ? 0 : this.String.GetHashCode();
        }

        public int CompareTo(StringWithConstraints other)
        {
            return this.CompareTo((object)other);
        }

        public int CompareTo(string other)
        {
            return this.CompareTo((object)other);
        }

        public bool Equals(StringWithConstraints other)
        {
            return this.Equals(other.ToStringEx(null));
        }

        public bool Equals(string other)
        {
            return this.String == other;
        }

        #endregion Overrides and interface implementations
    }

    /// <summary>
    /// Represents a string that is never "" or has only white spaces in it: it's either null or non-empty, non-whitespace string.
    /// </summary>
    public class NonEmptyString : StringWithConstraints
    {
        public NonEmptyString(string str)
            : base(str.IsBlank() ? null : str)
        {
        }

        public static implicit operator NonEmptyString(string str)
        {
            return new NonEmptyString(str);
        }
    }

    /// <summary>
    /// Represents a string that is either null, or non-empty/non-blank string with no leading or trailing white spaces.
    /// Empty string becomes null, non-empty strings get trimmed.
    /// </summary>
    public class NonEmptyTrimmedString : NonEmptyString
    {
        public NonEmptyTrimmedString(string str)
            : base(str == null ? null : str.Trim())
        {
        }

        public static implicit operator NonEmptyTrimmedString(string str)
        {
            return new NonEmptyTrimmedString(str);
        }
    }

    /// <summary>
    /// Represents a string that never has leading or trailing whites paces.
    /// </summary>
    public class TrimmedString : StringWithConstraints
    {
        public TrimmedString(string str)
            : base(str == null ? null : str.Trim())
        {
        }

        public static implicit operator TrimmedString(string str)
        {
            return new TrimmedString(str);
        }
    }

    /// <summary>
    /// String that can never be null. Null is converted into "".
    /// </summary>
    public class NonNullString : StringWithConstraints
    {
        public NonNullString(string str)
            : base(str == null ? string.Empty : str)
        {
        }

        public static implicit operator NonNullString(string str)
        {
            return new NonNullString(str);
        }
    }

    /// <summary>
    /// Base class for strings that must not exceed certain length.
    /// Create subclasses for biz logic-specific strings instead of using this class directly.
    /// </summary>
    public abstract class TruncatedString : StringWithConstraints
    {
        public string OriginalString { get; protected set; }

        public readonly int MaxLen;
        public readonly NonNullString Ellipsis = string.Empty;

        public TruncatedString(string str, uint maxLength, string optionalEllipsis = "...")
            : base(string.Empty)
        {
            this.MaxLen = (int)maxLength;
            this.Ellipsis = optionalEllipsis;

            this.String = str;
        }

        public override string String
        {
            get
            {
                return base.String;
            }
            protected set
            {
                this.OriginalString = value;
                base.String = value.TruncateIfExceed((uint)this.MaxLen, this.Ellipsis);
            }
        }
    }

    /// <summary>
    /// String that does not exceed 255 chars in length. When truncated, no ellipsis added.
    /// This class is an example of how TruncatedString subclasses should be created.
    /// </summary>
    public class String255 : TruncatedString
    {
        public String255(string str)
            : base(str, 255, optionalEllipsis: null)
        {
        }

        public static implicit operator String255(string str)
        {
            return new String255(str);
        }
    }
}
