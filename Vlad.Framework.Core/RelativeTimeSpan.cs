using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Aspectacular
{
    /*
     *            Time Unit Start    Reference Moment      Time Unit End
     *  Example:   (Day start)             (Now)              (Day End)
     * ----- Previous -->|<-- Current -------+-------- Current -->|<-- Next -------
     * ----------------->|<-------To Date -->|
     * ----------------------------- Past -->|<-- Future --------------------------
     * 
     */

    [Flags]
    public enum TimeUnits
    {
        UtcUnit = 0x10, // Time Unit that is better suited for UTC time ranges (like PAST xxx units). Typically, hours, minutes and seconds.
        Second = UtcUnit + 1,
        Minute = UtcUnit + 2,
        Hour = UtcUnit + 3,
        Eternity = UtcUnit + 4, // Open-ended past or future

        LocalTimeUnit = 0x100, // Time units that is better suited for Local (day-based) time ranges.
        Day = LocalTimeUnit + 1,
        Week = LocalTimeUnit + 2, 
        Month = LocalTimeUnit + 3, 
        Quarter = LocalTimeUnit + 4, 
        Year = LocalTimeUnit + 5, 
        Decade = LocalTimeUnit + 6, 
        Century = LocalTimeUnit + 7,
    }

    [Flags]
    public enum TimespanQualifiers
    {
        UtcTimespan = 0x10,
        Past = UtcTimespan + 1,
        Future = UtcTimespan + 2,

        LocalTimeSpan = 0x100, 
        AllowsOnlySingle = 0x1000,

        /// <summary>
        /// To date/till now. For example, year to date, hour till now.
        /// </summary>
        ToDate = LocalTimeSpan + UtcTimespan + AllowsOnlySingle + 0,

        CurrentOrSpecified = LocalTimeSpan + AllowsOnlySingle + 0,
        Previous = LocalTimeSpan + 1,
        Next = LocalTimeSpan + 2,
    }

    /// <summary>
    /// More intuitive time range specification, than start date - end date type of date/time range.
    /// For example, "Previous 3 quarters", "Current week", "Past 48 hours".
    /// </summary>
    public class RelativeTimeSpan
    {
        public readonly TimeUnits Unit;
        public readonly TimespanQualifiers Direction;
        public readonly ulong UnitCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="unit"></param>
        /// <param name="unitCount"></param>
        public RelativeTimeSpan(TimespanQualifiers direction, TimeUnits unit, ulong unitCount = 1)
        {
            if (unitCount == 0)
                throw new ArgumentOutOfRangeException("unitCount value must be 1 or greater.");

            if(!direction.IsCompatibleWith(unit))
                throw new ArgumentException("Time direction \"{0}\" cannot be used with the unit type \"{1}\".".SmartFormat(direction, unit));

            if(direction.HasFlag(TimespanQualifiers.AllowsOnlySingle) && unitCount > 1)
                throw new ArgumentException("Time direction \"{0}\" can only be used with unitCount=1.".SmartFormat(direction));

            this.UnitCount = unitCount;
            this.Unit = unit;
            this.Direction = direction;
        }

        public DateRange GetTimeValueRange(DateTime? referenceMoment = null)
        {
            if (this.Unit.HasFlag(TimeUnits.UtcUnit))
                throw new InvalidOperationException("Use GetTimeMomentRange() to get a range between two moments int time.");

#pragma warning disable 618
            return this.GetDateTimeRange(referenceMoment);
#pragma warning restore 618
        }

        [Obsolete("Use either GetTimeValueRange() or GetTimeMomentRange() instead.")]
        public DateRange GetDateTimeRange(DateTime? referenceMoment)
        {
            DateTime refMoment = referenceMoment == null || referenceMoment.Value == default(DateTime) ? 
                        (this.Unit.GetKind() == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now) 
                        : referenceMoment.Value;

            DateTime? start = null, end = null;

            switch(this.Direction)
            {
                case TimespanQualifiers.CurrentOrSpecified:
                    start = refMoment.StartOf(this.Unit);
                    end = refMoment.EndOf(this.Unit);
                    break;
                case TimespanQualifiers.ToDate:
                    start = refMoment.StartOf(this.Unit);
                    end = refMoment; // refMoment.EndOf(TimeUnits.Day);
                    break;
                case TimespanQualifiers.Past:
                    Debug.Assert(this.Unit.HasFlag(TimeUnits.UtcUnit));
                    end = refMoment;
                    start = this.Unit == TimeUnits.Eternity ? (DateTime?)null : end.Value.Add(-(int)this.UnitCount, this.Unit);
                    break;
                case TimespanQualifiers.Future:
                    Debug.Assert(this.Unit.HasFlag(TimeUnits.UtcUnit));
                    start = refMoment;
                    end = this.Unit == TimeUnits.Eternity ? (DateTime?)null : start.Value.Add((int)this.UnitCount, this.Unit);
                    break;
                case TimespanQualifiers.Previous:
                    end = refMoment.StartOf(this.Unit);
                    start = end.Value.Add(-(int)this.UnitCount, this.Unit);
                    end = end.Value.PreviousMoment();
                    break;
                case TimespanQualifiers.Next:
                    start = refMoment.StartOf(this.Unit).Add(1, this.Unit);
                    end = start.Value.Add((int)this.UnitCount, this.Unit);
                    break;
            }

            DateRange range = new DateRange(start, end);
            return range;
        }

        public TimeMomentRange GetTimeMomentRange(DateTimeOffset? referenceMoment = null)
        {
            if (!this.Unit.HasFlag(TimeUnits.UtcUnit))
                throw new InvalidOperationException("Use GetDateTimeRange() to get a range between two time values.");

            DateTime? refMoment = referenceMoment == null ? (DateTime?)null : referenceMoment.Value.UtcDateTime;
#pragma warning disable 618
            DateRange derange = this.GetDateTimeRange(refMoment);
#pragma warning restore 618

            DateTimeOffset? starto, endo;

            if (referenceMoment == null)
            {
                starto = derange.HasStart ? derange.Start.Value : (DateTimeOffset?)null;
                endo = derange.HasEnd ? derange.End.Value : (DateTimeOffset?)null;
            }else
            {
                TimeSpan utcOffset = referenceMoment.Value.Offset;

                starto = derange.HasStart ? new DateTimeOffset(derange.Start.Value, utcOffset) : (DateTimeOffset?)null;
                endo = derange.HasEnd ? new DateTimeOffset(derange.End.Value, utcOffset) : (DateTimeOffset?)null;
            }

            TimeMomentRange range = new TimeMomentRange(starto, endo);
            return range;
        }
    }

    public static class RelativeTimeSpanExtensions
    {
        public static DateTimeKind GetKind(this TimeUnits unit)
        {
            if ((unit & TimeUnits.UtcUnit) != 0)
                return DateTimeKind.Utc;
            if ((unit & TimeUnits.LocalTimeUnit) != 0)
                return DateTimeKind.Local;

            return DateTimeKind.Unspecified;
        }

        public static bool IsCompatibleWith(this TimespanQualifiers direction, TimeUnits unit)
        {
            int directionFlags = (int)(direction & (TimespanQualifiers.UtcTimespan | TimespanQualifiers.LocalTimeSpan));
            int unitFlags = (int)(unit & (TimeUnits.UtcUnit | TimeUnits.LocalTimeUnit));

            return (directionFlags & unitFlags) != 0;
        }

        /// <summary>
        /// Returns higher time Unit in which the given Unit is *repeated within*.
        /// This is not a direct hierarchy! Different types of units may have same parent.
        /// For example, Month number 1..12 is repeated within a Year, so Month's parent is Year, not quarter.
        /// Quarter 1..4 also is repeated within a year, so Quarter's parent is also year.
        /// Same goes for Week number 1..52.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeUnits? CalculationParent(this TimeUnits unit)
        {
            switch(unit)
            {
                case TimeUnits.Century:
                    return null; // no parent.
                case TimeUnits.Decade:
                    return TimeUnits.Century;
                case TimeUnits.Year:
                    return null; // no parent.
                case TimeUnits.Quarter:
                    return TimeUnits.Year;
                case TimeUnits.Month:
                    return TimeUnits.Year;
                case TimeUnits.Week:
                    return TimeUnits.Year;
                case TimeUnits.Day:
                    return TimeUnits.Month;
                case TimeUnits.Hour:
                    return TimeUnits.Day;
                case TimeUnits.Minute:
                    return TimeUnits.Hour;
                case TimeUnits.Second:
                    return TimeUnits.Minute;
            }

            throw new Exception("Parent of \"{0}\" is not specified.".SmartFormat(unit));
        }

        public static DateTime StartOf(this DateTime dt, TimeUnits unit)
        {
            DateTimeKind dtKind = dt.Kind == DateTimeKind.Unspecified ? DateTimeKind.Local : dt.Kind;

            switch (unit)
            {
                case TimeUnits.Century:
                    {
                        int year = dt.Year / 100 * 100;
                        return new DateTime(year, 1, 1, 0, 0, 0, dtKind);
                    }
                case TimeUnits.Decade:
                    {
                        int year = dt.Year / 10 * 10;
                        return new DateTime(year, 1, 1, 0, 0, 0, dtKind);
                    }
                case TimeUnits.Year:
                    {
                        return new DateTime(dt.Year, 1, 1, 0, 0, 0, dtKind);
                    }
                case TimeUnits.Quarter:
                    {
                        int month = (dt.Quarter() - 1) * 3 + 1;
                        return new DateTime(dt.Year, month, 1, 0, 0, 0, dtKind);
                    }
                case TimeUnits.Week:
                    {
                        DayOfWeek weekStart = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                        int delta = weekStart - dt.DayOfWeek;
                        return dt.AddDays(delta).StartOf(TimeUnits.Day);
                    }
                case TimeUnits.Month:
                    {
                        return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, dtKind);
                    }
                case TimeUnits.Day:
                    {
                        return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, dtKind);
                    }
                case TimeUnits.Hour:
                    {
                        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, dtKind);
                    }
                case TimeUnits.Minute:
                    {
                        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dtKind);
                    }
                case TimeUnits.Second:
                    {
                        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dtKind);
                    }
            }

            throw new Exception("Calculation of start of unit \"{0}\" is not implemented.".SmartFormat(unit));
        }

        public static DateTime Add(this DateTime dt, int count, TimeUnits unit)
        {
            switch (unit)
            {
                case TimeUnits.Century:
                    return dt.AddYears(count * 100);
                case TimeUnits.Day:
                    return dt.AddDays(count);
                case TimeUnits.Decade:
                    return dt.AddYears(count * 10);
                case TimeUnits.Hour:
                    return dt.AddHours(count);
                case TimeUnits.Minute:
                    return dt.AddMinutes(count);
                case TimeUnits.Month:
                    return dt.AddMonths(count);
                case TimeUnits.Quarter:
                    return dt.AddMonths(count * 3);
                case TimeUnits.Second:
                    return dt.AddSeconds(count);
                case TimeUnits.Week:
                    return dt.AddDays(count * 7);
                case TimeUnits.Year:
                    return dt.AddYears(count);
            }

            throw new Exception("Adding \"{0}\" is not implemented.".SmartFormat(unit));
        }

        public static DateTime EndOf(this DateTime dt, TimeUnits unit)
        {
            DateTime start = dt.StartOf(unit);
            DateTime next = start.Add(1, unit);
            DateTime end = next.PreviousMoment();
            return end;
        }

        #region DateRanage factory methods

        public static DateRange Current(this TimeUnits unit, DateTime? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(TimespanQualifiers.CurrentOrSpecified, unit);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange ToDate(this TimeUnits unit, DateTime? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(TimespanQualifiers.ToDate, unit);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange Past(this TimeUnits unit, ulong unitCount = 1, DateTime? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(TimespanQualifiers.Past, unit, unitCount);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange Future(this TimeUnits unit, ulong unitCount = 1, DateTime? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(TimespanQualifiers.Future, unit, unitCount);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange Previous(this TimeUnits unit, ulong unitCount = 1, DateTime? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(TimespanQualifiers.Previous, unit, unitCount);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange Next(this TimeUnits unit, ulong unitCount = 1, DateTime? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(TimespanQualifiers.Next, unit, unitCount);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }


        public static DateRange RangeCurrent(this DateTime dt, TimeUnits unit)
        {
            return unit.Current(dt);
        }

        public static DateRange RangeCurrent(this DateTime? dt, TimeUnits unit)
        {
            return unit.Current(dt);
        }

        public static DateRange RangeToDate(this DateTime dt, TimeUnits unit)
        {
            return unit.ToDate(dt);
        }

        public static DateRange RangeToDate(this DateTime? dt, TimeUnits unit)
        {
            return unit.ToDate(dt);
        }

        public static DateRange RangePast(this DateTime dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Past(unitCount, dt);
        }

        public static DateRange RangePast(this DateTime? dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Past(unitCount, dt);
        }

        public static DateRange RangeFuture(this DateTime dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Future(unitCount, dt);
        }

        public static DateRange RangeFuture(this DateTime? dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Future(unitCount, dt);
        }

        public static DateRange RangePrevious(this DateTime dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Previous(unitCount, dt);
        }

        public static DateRange RangePrevious(this DateTime? dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Previous(unitCount, dt);
        }

        public static DateRange RangeNext(this DateTime dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Next(unitCount, dt);
        }

        public static DateRange RangeNext(this DateTime? dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Next(unitCount, dt);
        }

        #endregion DateRanage factory methods
    }
}
