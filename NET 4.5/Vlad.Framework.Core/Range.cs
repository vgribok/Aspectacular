#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Aspectacular
{
    /// <summary>
    ///     Class representing an inclusive range between two comparable objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public abstract class RangeBase<T> : IEquatable<RangeBase<T>>, ISerializable
        where T : IComparable
    {
        protected object start = null;
        protected object end = null;

        /// <summary>
        ///     Range's inclusive lower bound.
        /// </summary>
        public virtual T Start
        {
            get { return (T)this.start; }
            set
            {
                this.start = value;
                this.CheckOrder();
            }
        }

        /// <summary>
        ///     Range's inclusive higher bound.
        /// </summary>
        public virtual T End
        {
            get { return (T)this.end; }
            set
            {
                this.end = value;
                this.CheckOrder();
            }
        }

        protected void CheckOrder()
        {
            if(this.start != null && this.end != null)
            {
                int comparisonResult = ((T)this.start).CompareTo(this.end);
                if(comparisonResult > 0) // swap required
                {
                    object temp = this.start;
                    this.start = this.end;
                    this.end = temp;
                }
            }
        }

        protected RangeBase() : this(null, null)
        {
        }

        protected RangeBase(object start, object end)
        {
            this.start = start;
            this.end = end;
            this.CheckOrder();
        }

        /// <summary>
        ///     Returns false if range is open-ended on the left.
        /// </summary>
        [XmlIgnore]
        public virtual bool HasStart
        {
            get { return this.start != null; }
        }

        /// <summary>
        ///     Returns false if range is open-ended on the right.
        /// </summary>
        [XmlIgnore]
        public virtual bool HasEnd
        {
            get { return this.end != null; }
        }

        /// <summary>
        ///     Returns true if either Start or End are not specified.
        /// </summary>
        [XmlIgnore]
        public bool IsOpenEnded
        {
            get { return !this.HasStart || !this.HasEnd; }
        }

        public override bool Equals(object obj)
        {
            RangeBase<T> other = obj as RangeBase<T>;
            if(other != null)
                return this.Equals(other);

            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        public bool Equals(RangeBase<T> other)
        {
            if(other == null)
                return false;

            if(this.HasStart != other.HasStart || this.HasEnd != other.HasEnd)
                return false;

            object startObj = this.Start;

            if(startObj == null)
            {
                object otherStart = other.Start;
                if(otherStart != null)
                    return false;
            } else
            {
                if(!this.Start.Equals(other.Start))
                    return false;
            }

            object endObj = this.End;
            if(endObj == null)
            {
                object otherEnd = other.End;
                if(otherEnd != null)
                    return false;
            } else
            {
                if(!this.End.Equals(other.End))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
// ReSharper disable NonReadonlyFieldInGetHashCode
            int hash1 = this.start == null ? 0 : this.start.GetHashCode();
            int hash2 = this.end == null ? 0 : this.end.GetHashCode();
// ReSharper restore NonReadonlyFieldInGetHashCode
            int hash = hash1 ^ hash2;
            return hash;
        }

        /// <summary>
        ///     Returns true if given value lies within the range.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool Contains(T val)
        {
            bool equalOrGreaterThanStart = !this.HasStart || this.Start.CompareTo(val) <= 0;
            if(equalOrGreaterThanStart)
            {
                bool equalOrLessThanEnd = !this.HasEnd || this.End.CompareTo(val) >= 0;
                if(equalOrLessThanEnd)
                    return true;
            }

            return false;
        }

        public override string ToString()
        {
            string startStr = this.start.ToStringEx("NULL");
            string endStr = this.end.ToStringEx("NULL");

            return "{{ {0} : {1} }}".SmartFormat(startStr, endStr);
        }

        // ReSharper disable once UnusedParameter.Local
        protected RangeBase(SerializationInfo info, StreamingContext ctxt)
        {
            this.start = info.GetValue("Start", typeof(T));
            this.end = info.GetValue("End", typeof(T));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Start", this.Start);
            info.AddValue("End", this.End);
            this.CheckOrder();
        }
    }

    /// <summary>
    ///     Range of reference types. May be used for strings.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class Range<T> : RangeBase<T>
        where T : class, IComparable
    {
        public Range() : this(null, null)
        {
        }

        public Range(T start, T end)
            : base(start, end)
        {
        }

        // ReSharper disable once UnusedMember.Local
        protected Range(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }
    }

    /// <summary>
    ///     Range of value types. My be used for numerical types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class ValueRange<T> : RangeBase<T>
        where T : struct, IComparable
    {
        public ValueRange() : this(null, null)
        {
        }

        public ValueRange(T? start, T? end)
            : base(start == null ? null : (object)start.Value, end == null ? null : (object)end.Value)
        {
        }


        // ReSharper disable once UnusedParameter.Local
        protected ValueRange(SerializationInfo info, StreamingContext ctxt)
        {
            this.start = info.GetValue("Start", typeof(T?));
            this.end = info.GetValue("End", typeof(T?));
            this.CheckOrder();
        }

        //[XmlIgnore]
        public new T? Start
        {
            get { return this.start == null ? (T?)null : (T)this.start; }
            set
            {
                this.start = value;
                this.CheckOrder();
            }
        }

        //[XmlIgnore]
        public new T? End
        {
            get { return this.end == null ? (T?)null : (T)this.end; }
            set
            {
                this.end = value;
                this.CheckOrder();
            }
        }
    }

    public static class RangeFactory
    {
        /// <summary>
        ///     Factory method for simplified instantiation of Range class.
        /// </summary>
        /// <typeparam name="T">Reference type, like string.</typeparam>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static Range<T> CreateRange<T>(T start, T end)
            where T : class, IComparable
        {
            return new Range<T>(start, end);
        }

        /// <summary>
        ///     Factory method for simplified instantiation of Range class.
        /// </summary>
        /// <typeparam name="T">value type, like int, decimal, etc.</typeparam>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [Obsolete("Please use CreateValueRange<T>(start, end) instead.")]
        public static ValueRange<T> CreateRange<T>(T? start, T? end)
            where T : struct, IComparable
        {
            return new ValueRange<T>(start, end);
        }

        /// <summary>
        ///     Factory method for simplified instantiation of Range class.
        /// </summary>
        /// <typeparam name="T">value type, like int, decimal, etc.</typeparam>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static ValueRange<T> CreateValueRange<T>(T? start, T? end)
            where T : struct, IComparable
        {
            return new ValueRange<T>(start, end);
        }


        /// <summary>
        ///     Factory method for simplified instantiation of Range class.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static DateRange CreateRange(DateTime? start, DateTime? end)
        {
            return new DateRange(start, end);
        }
    }

    /// <summary>
    ///     Class representing a range between two moments in time.
    /// </summary>
    [Serializable]
    public class TimeMomentRange : ValueRange<DateTimeOffset>
    {
        public TimeMomentRange()
// ReSharper disable RedundantCast
            : this((DateTimeOffset?)null, (DateTimeOffset?)null)
// ReSharper restore RedundantCast
        {
        }

        public TimeMomentRange(DateTimeOffset? start, DateTimeOffset? end)
            : base(start, end)
        {
        }

        protected TimeMomentRange(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        /// <summary>
        ///     Returns null if range is open-ended (Start or End is null). Otherwise returns TimeSpan.
        /// </summary>
        public static implicit operator TimeSpan?(TimeMomentRange range)
        {
            return range.IsOpenEnded ? (TimeSpan?)null : range.End.Value - range.Start.Value;
        }

        /// <summary>
        ///     Returns null if range is open-ended (Start or End is null). Otherwise returns TimeSpan.
        /// </summary>
        [XmlIgnore]
        public TimeSpan? Span
        {
            get { return this; }
        }
    }

    /// <summary>
    ///     Class representing a range of date/time values.
    /// </summary>
    [Serializable]
    public class DateRange : ValueRange<DateTime>
    {
        public DateRange()
// ReSharper disable RedundantCast
            : this((DateTime?)null, (DateTime?)null)
// ReSharper restore RedundantCast
        {
        }

        public DateRange(DateTime? start, DateTime? end)
            : base(start, end)
        {
        }

        protected DateRange(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        /// <summary>
        ///     Returns, whether date range is UTC, Local, or unspecified.
        /// </summary>
        [XmlIgnore]
        public DateTimeKind Kind
        {
            get
            {
                // ReSharper disable PossibleInvalidOperationException
                DateTimeKind? startKind = this.HasStart ? Start.Value.Kind : (DateTimeKind?)null;
                DateTimeKind? endKind = this.HasEnd ? this.End.Value.Kind : (DateTimeKind?)null;
                // ReSharper restore PossibleInvalidOperationException

                if(startKind == null && endKind == null)
                    return DateTimeKind.Unspecified;

                if(startKind == null)
                    return endKind.Value;
                if(endKind == null)
                    return startKind.Value;

                return startKind.Value == endKind.Value ? startKind.Value : DateTimeKind.Unspecified;
            }
        }

        public DateRange ToUtc()
        {
            if(this.Kind == DateTimeKind.Utc)
                return this;

            // ReSharper disable PossibleInvalidOperationException
            DateTime? newStart = this.HasStart ? this.Start.Value.ToUniversalTime() : (DateTime?)null;
            DateTime? newEnd = this.HasEnd ? this.End.Value.ToUniversalTime() : (DateTime?)null;
            // ReSharper restore PossibleInvalidOperationException

            return new DateRange(newStart, newEnd);
        }

        public DateRange ToLocal()
        {
            if(this.Kind == DateTimeKind.Local)
                return this;

            // ReSharper disable PossibleInvalidOperationException
            DateTime? newStart = this.HasStart ? this.Start.Value.ToLocalTime() : (DateTime?)null;
            DateTime? newEnd = this.HasEnd ? this.End.Value.ToLocalTime() : (DateTime?)null;
            // ReSharper restore PossibleInvalidOperationException

            return new DateRange(newStart, newEnd);
        }

        /// <summary>
        ///     Returns null if range is open-ended (Start or End is null). Otherwise returns TimeSpan.
        /// </summary>
        public static implicit operator TimeSpan?(DateRange range)
        {
            return range.IsOpenEnded ? (TimeSpan?)null : range.End.Value - range.Start.Value;
        }

        /// <summary>
        ///     Returns null if range is open-ended (Start or End is null). Otherwise returns TimeSpan.
        /// </summary>
        [XmlIgnore]
        public TimeSpan? Span
        {
            get { return this; }
        }

        //public static DateRange operator +(DateRange range, TimeSpan unitCount)
        //{
        //    DateTime? newStart = range.HasStart ? range.Start.Value + unitCount : (DateTime?)null;
        //    DateTime? newEnd = range.HasEnd ? range.End.Value + unitCount : (DateTime?)null;

        //    return new DateRange(newStart, newEnd);
        //}

        //public static DateRange operator -(DateRange range, TimeSpan unitCount)
        //{
        //    DateTime? newStart = range.HasStart ? range.Start.Value - unitCount : (DateTime?)null;
        //    DateTime? newEnd = range.HasEnd ? range.End.Value - unitCount : (DateTime?)null;

        //    return new DateRange(newStart, newEnd);
        //}
    }
}