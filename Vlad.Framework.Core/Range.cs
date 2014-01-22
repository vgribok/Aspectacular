using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aspectacular
{
    public abstract class RangeBase<T> : IEquatable<RangeBase<T>> 
        where T : IComparable
    {
        protected readonly object start = null;
        protected readonly object end = null;

        public T Start 
        { 
            get { return (T)this.start; }
        }
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

        public virtual bool HasStart
        {
            get { return this.start != null; }
        }

        public virtual bool HasEnd
        {
            get { return this.end != null; }
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

    public class Range<T> : RangeBase<T>
        where T : class, IComparable
    {
        public Range(T start, T end)
            : base(start, end)
        {
        }
    }

    public class ValueRange<T> : RangeBase<T>
        where T : struct, IComparable
    {
        public ValueRange(T? start, T? end)
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
        public static Range<T> CreateRange<T>(T start, T end)
            where T : class, IComparable
        {
            return new Range<T>(start, end);
        }

        public static ValueRange<T> CreateRange<T>(T? start, T? end)
            where T : struct, IComparable
        {
            return new ValueRange<T>(start, end);
        }
    }

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
    }
}
