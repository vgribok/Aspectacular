﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aspectacular
{
    /// <summary>
    /// Class representing an inclusive range between two comparable objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RangeBase<T> : IEquatable<RangeBase<T>> 
        where T : IComparable
    {
        protected readonly object start = null;
        protected readonly object end = null;

        /// <summary>
        /// Range's inclusive lower bound.
        /// </summary>
        public T Start 
        { 
            get { return (T)this.start; }
        }
        
        /// <summary>
        /// Range's inclusive higher bound. 
        /// </summary>
        public T End 
        {
            get { return (T)this.end; } 
        }

        public RangeBase(object start, object end)
        {
            object startObj = start;
            object endObj = end;

            if (startObj == null || endObj == null)
            {
                this.start = start;
                this.end = end;
            }else
            {
                int comparisonResult = ((T)start).CompareTo(end);
                this.start = comparisonResult <= 0 ? start : end;
                this.end = comparisonResult <= 0 ? end : start;
            }
        }

        /// <summary>
        /// Returns false if range is open-ended on the left.
        /// </summary>
        public virtual bool HasStart
        {
            get { return this.start != null; }
        }

        /// <summary>
        /// Returns false if range is open-ended on the right.
        /// </summary>
        public virtual bool HasEnd
        {
            get { return this.end != null; }
        }

        public override bool Equals(object obj)
        {
            RangeBase<T> other = obj as RangeBase<T>;
            if (other != null)
                return this.Equals(other);

            return base.Equals(obj);
        }

        public bool Equals(RangeBase<T> other)
        {
            if (other == null)
                return false;

            if (this.HasStart != other.HasStart || this.HasEnd != other.HasEnd)
                return false;

            object startObj = this.Start;

            if (startObj == null)
            {
                object otherStart = other.Start;
                if (otherStart != null)
                    return false;
            }else
            {
                if (!this.Start.Equals(other.Start))
                    return false;
            }

            object endObj = this.End;
            if (endObj == null)
            {
                object otherEnd = other.End;
                if (otherEnd != null)
                    return false;
            }else
            {
                if (!this.End.Equals(other.End))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if given value lies within the range.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool Contains(T val)
        {
            bool equalOrGreaterThanStart = !this.HasStart || this.Start.CompareTo(val) <= 0;
            if (equalOrGreaterThanStart)
            {
                bool equalOrLessThanEnd = !this.HasEnd || this.End.CompareTo(val) >= 0;
                if (equalOrLessThanEnd)
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
    }

    /// <summary>
    /// Range of reference types. May be used for strings.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Range<T> : RangeBase<T>
        where T : class, IComparable
    {
        internal protected Range(T start, T end)
            : base(start, end)
        {
        }
    }

    /// <summary>
    /// Range of value types. My be used for numerical types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueRange<T> : RangeBase<T>
        where T : struct, IComparable
    {
        internal protected ValueRange(T? start, T? end)
            : base(start == null ? (object)null : (object)start.Value, end == null ? (object)null : (object)end.Value)
        {
        }

        public new T? Start
        {
            get { return this.start == null ? (T?)null : (T)this.start; }
        }
        public new T? End
        {
            get { return this.end == null ? (T?)null : (T)this.end; }
        }
    }

    public static class RangeFactory
    {
        /// <summary>
        /// Factory method for simplified instantiation of Range class.
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
        /// Factory method for simplified instantiation of Range class.
        /// </summary>
        /// <typeparam name="T">value type, like int, decimal, etc.</typeparam>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static ValueRange<T> CreateRange<T>(T? start, T? end)
            where T : struct, IComparable
        {
            return new ValueRange<T>(start, end);
        }

        /// <summary>
        /// Factory method for simplified instantiation of Range class.
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
    /// Class representing a range of date/time values.
    /// </summary>
    public class DateRange : ValueRange<DateTime>
    {
        public DateRange(DateTime? start, DateTime? end)
            : base(start, end)
        {
        }

        public DateRange ToUtc()
        {
            DateTime? newStart = this.HasStart ? this.Start.Value.ToUniversalTime() : (DateTime?)null;
            DateTime? newEnd = this.HasEnd ? this.End.Value.ToUniversalTime() : (DateTime?)null;

            return new DateRange(newStart, newEnd);
        }

        public DateRange ToLocal()
        {
            DateTime? newStart = this.HasStart ? this.Start.Value.ToLocalTime() : (DateTime?)null;
            DateTime? newEnd = this.HasEnd ? this.End.Value.ToLocalTime() : (DateTime?)null;

            return new DateRange(newStart, newEnd);
        }

        //public static DateRange operator +(DateRange range, TimeSpan span)
        //{
        //    DateTime? newStart = range.HasStart ? range.Start.Value + span : (DateTime?)null;
        //    DateTime? newEnd = range.HasEnd ? range.End.Value + span : (DateTime?)null;

        //    return new DateRange(newStart, newEnd);
        //}

        //public static DateRange operator -(DateRange range, TimeSpan span)
        //{
        //    DateTime? newStart = range.HasStart ? range.Start.Value - span : (DateTime?)null;
        //    DateTime? newEnd = range.HasEnd ? range.End.Value - span : (DateTime?)null;

        //    return new DateRange(newStart, newEnd);
        //}
    }
}
