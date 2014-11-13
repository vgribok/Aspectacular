#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Globalization;

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
    public enum TimelineFlags
    {
        /// <summary>
        ///     Marks timeline elements that can have only one time unit.
        ///     For example, current
        /// </summary>
        AllowsOnlySingleUnit = 0x1000,

        /// <summary>
        ///     Modifier that includes
        /// </summary>
        IncludeEntireCurrentOrSpecified = 0x0001,

        IncludeNowOrSpecified = 0x4000,

        /// <summary>
        ///     Time unit or timeline element represents moment in time rather than date/time value.
        /// </summary>
        MomentInTime = 0x2000,
    }

    public enum TimeUnits
    {
        Second = TimelineFlags.MomentInTime + 1,
        Minute,
        Hour,
        Eternity, // Open-ended past or future

        Day = 1,
        Week,
        Month,
        Quarter,
        Year,
        Decade,
        Century,
    }

    [Flags]
    public enum Timeline
    {
        /// <summary>
        ///     Current/specified day, hour, year, second, month, etc.
        /// </summary>
        EntireCurrentOrSpecified = TimelineFlags.AllowsOnlySingleUnit + TimelineFlags.IncludeEntireCurrentOrSpecified,

        /// <summary>
        ///     Before current one.
        ///     Example: if now is September, then 3 x PreviousExcludingCurrent will return a span encompassing June, July and
        ///     August.
        /// </summary>
        PreviousExcludingCurrent = 2,

        /// <summary>
        ///     Example: if now is September 15, then 3 x Past will return a span encompassing entire July, August and September up
        ///     to Sept 15th.
        /// </summary>
        Past = PreviousExcludingCurrent | TimelineFlags.IncludeNowOrSpecified,

        /// <summary>
        ///     Month-to-date, year-to-date, day-till-now.
        /// </summary>
        ToDateOrTillSpecified = Past | TimelineFlags.AllowsOnlySingleUnit,

        /// <summary>
        ///     The one after current
        ///     Example: if now is September, then 3 x NextExcludingCurrent will return a span encompassing October, November and
        ///     December.
        /// </summary>
        NextExcludingCurrent = 4,

        /// <summary>
        ///     Example: if now is September 15, then 3 x Future will return a span encompassing Sept after 15th, October and
        ///     entire November.
        /// </summary>
        Future = NextExcludingCurrent | TimelineFlags.IncludeNowOrSpecified,
    }

    /// <summary>
    ///     More intuitive time range specification, than start date - end date type of date/time range.
    ///     For example, "PreviousExcludingCurrent 3 quarters", "Current week", "Past 48 hours".
    /// </summary>
    public class RelativeTimeSpan
    {
        public readonly TimeUnits Unit;
        public readonly Timeline Direction;
        public readonly ulong UnitCount;

        /// <summary>
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="unit"></param>
        /// <param name="unitCount"></param>
        public RelativeTimeSpan(Timeline direction, TimeUnits unit, ulong unitCount = 1)
        {
            if(unitCount == 0)
                throw new ArgumentOutOfRangeException("unitCount value must be 1 or greater.");

            if(((int)direction & (int)TimelineFlags.AllowsOnlySingleUnit) != 0 && unitCount > 1)
                throw new ArgumentException("Time direction \"{0}\" can only be used with unitCount=1.".SmartFormat(direction));

            this.UnitCount = unitCount;
            this.Unit = unit;
            this.Direction = direction;
        }

        public DateRange GetDateTimeRange(DateTime? referenceMoment)
        {
            DateTime refMoment = referenceMoment.IsNullOrDefault() ? (this.Unit.IsMomentInTime() ? DateTime.UtcNow : DateTime.Now) : referenceMoment.Value;

            DateTime? start = null, end = null;

            switch(this.Direction)
            {
                case Timeline.EntireCurrentOrSpecified:
                    start = refMoment.StartOf(this.Unit);
                    end = refMoment.EndOf(this.Unit);
                    break;
                case Timeline.ToDateOrTillSpecified:
                    start = refMoment.StartOf(this.Unit);
                    end = refMoment; // refMoment.EndOf(TimeUnits.Day);
                    break;
                case Timeline.Past:
                    end = refMoment;
                    start = this.Unit == TimeUnits.Eternity ? (DateTime?)null : end.Value.Add(-(int)this.UnitCount, this.Unit);
                    break;
                case Timeline.Future:
                    start = refMoment;
                    end = this.Unit == TimeUnits.Eternity ? (DateTime?)null : start.Value.Add((int)this.UnitCount, this.Unit);
                    break;
                case Timeline.PreviousExcludingCurrent:
                    end = refMoment.StartOf(this.Unit);
                    start = end.Value.Add(-(int)this.UnitCount, this.Unit);
                    end = end.Value.PreviousMoment();
                    break;
                case Timeline.NextExcludingCurrent:
                    start = refMoment.StartOf(this.Unit).Add(1, this.Unit);
                    end = start.Value.Add((int)this.UnitCount, this.Unit);
                    break;
            }

            DateRange range = new DateRange(start, end);
            return range;
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="referenceMoment"></param>
        /// <returns></returns>
        public TimeMomentRange GetTimeMomentRange(DateTimeOffset? referenceMoment = null)
        {
            DateTime? refMoment = referenceMoment == null ? (DateTime?)null : new DateTime(referenceMoment.Value.UtcDateTime.Ticks);
#pragma warning disable 618
            DateRange derange = this.GetDateTimeRange(refMoment);
#pragma warning restore 618

            DateTimeOffset? starto, endo;

            if(referenceMoment == null)
            {
                starto = derange.HasStart ? derange.Start.Value : (DateTimeOffset?)null;
                endo = derange.HasEnd ? derange.End.Value : (DateTimeOffset?)null;
            } else
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
        public static bool IsMomentInTime(this TimeUnits unit)
        {
            return ((int)unit & (int)TimelineFlags.MomentInTime) != 0;
        }

        /// <summary>
        ///     Returns higher time Unit in which the given Unit is *repeated within*.
        ///     This is not a direct hierarchy! Different types of units may have same parent.
        ///     For example, Month number 1..12 is repeated within a Year, so Month's parent is Year, not quarter.
        ///     Quarter 1..4 also is repeated within a year, so Quarter's parent is also year.
        ///     Same goes for Week number 1..52.
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
            switch (unit)
            {
                case TimeUnits.Century:
                    {
                        int year = dt.Year / 100 * 100;
                        return new DateTime(year, 1, 1, 0, 0, 0, dt.Kind);
                    }
                case TimeUnits.Decade:
                    {
                        int year = dt.Year / 10 * 10;
                        return new DateTime(year, 1, 1, 0, 0, 0, dt.Kind);
                    }
                case TimeUnits.Year:
                    {
                        return new DateTime(dt.Year, 1, 1, 0, 0, 0, dt.Kind);
                    }
                case TimeUnits.Quarter:
                    {
                        int month = (dt.Quarter() - 1) * 3 + 1;
                        return new DateTime(dt.Year, month, 1, 0, 0, 0, dt.Kind);
                    }
                case TimeUnits.Week:
                    {
                        DayOfWeek weekStart = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                        int delta = weekStart - dt.DayOfWeek;
                        return dt.AddDays(delta).StartOf(TimeUnits.Day);
                    }
                case TimeUnits.Month:
                    {
                        return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, dt.Kind);
                    }
                case TimeUnits.Day:
                    {
                        return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, dt.Kind);
                    }
                case TimeUnits.Hour:
                    {
                        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, dt.Kind);
                    }
                case TimeUnits.Minute:
                    {
                        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
                    }
                case TimeUnits.Second:
                    {
                        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
                    }
            }

            throw new Exception("Calculation of start of unit \"{0}\" is not implemented.".SmartFormat(unit));
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static DateTimeOffset StartOf(this DateTimeOffset dt, TimeUnits unit)
        {
            switch(unit)
            {
                case TimeUnits.Century:
                {
                    int year = dt.Year/100*100;
                    return new DateTimeOffset(year, 1, 1, 0, 0, 0, dt.Offset);
                }
                case TimeUnits.Decade:
                {
                    int year = dt.Year/10*10;
                    return new DateTimeOffset(year, 1, 1, 0, 0, 0, dt.Offset);
                }
                case TimeUnits.Year:
                {
                    return new DateTimeOffset(dt.Year, 1, 1, 0, 0, 0, dt.Offset);
                }
                case TimeUnits.Quarter:
                {
                    int month = (dt.Quarter() - 1)*3 + 1;
                    return new DateTimeOffset(dt.Year, month, 1, 0, 0, 0, dt.Offset);
                }
                case TimeUnits.Week:
                {
                    DayOfWeek weekStart = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                    int delta = weekStart - dt.DayOfWeek;
                    return dt.AddDays(delta).StartOf(TimeUnits.Day);
                }
                case TimeUnits.Month:
                {
                    return new DateTimeOffset(dt.Year, dt.Month, 1, 0, 0, 0, dt.Offset);
                }
                case TimeUnits.Day:
                {
                    return new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, dt.Offset);
                }
                case TimeUnits.Hour:
                {
                    return new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, dt.Offset);
                }
                case TimeUnits.Minute:
                {
                    return new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Offset);
                }
                case TimeUnits.Second:
                {
                    return new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Offset);
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


        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="count"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static DateTimeOffset Add(this DateTimeOffset dt, int count, TimeUnits unit)
        {
            switch(unit)
            {
                case TimeUnits.Century:
                    return dt.AddYears(count*100);
                case TimeUnits.Day:
                    return dt.AddDays(count);
                case TimeUnits.Decade:
                    return dt.AddYears(count*10);
                case TimeUnits.Hour:
                    return dt.AddHours(count);
                case TimeUnits.Minute:
                    return dt.AddMinutes(count);
                case TimeUnits.Month:
                    return dt.AddMonths(count);
                case TimeUnits.Quarter:
                    return dt.AddMonths(count*3);
                case TimeUnits.Second:
                    return dt.AddSeconds(count);
                case TimeUnits.Week:
                    return dt.AddDays(count*7);
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

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static DateTimeOffset EndOf(this DateTimeOffset dt, TimeUnits unit)
        {
            DateTimeOffset start = dt.StartOf(unit);
            DateTimeOffset next = start.Add(1, unit);
            DateTimeOffset end = next.PreviousMoment();
            return end;
        }

        #region TimeMomentRange factory methods

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="referenceMoment"></param>
        /// <returns></returns>
        public static TimeMomentRange Current(this TimeUnits unit, DateTimeOffset? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(Timeline.EntireCurrentOrSpecified, unit);
            TimeMomentRange range = span.GetTimeMomentRange(referenceMoment);
            return range;
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="referenceMoment"></param>
        /// <returns></returns>
        public static TimeMomentRange ToDate(this TimeUnits unit, DateTimeOffset? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(Timeline.ToDateOrTillSpecified, unit);
            TimeMomentRange range = span.GetTimeMomentRange(referenceMoment);
            return range;
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="unitCount"></param>
        /// <param name="referenceMoment"></param>
        /// <returns></returns>
        public static TimeMomentRange Past(this TimeUnits unit, ulong unitCount = 1, DateTimeOffset? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(Timeline.Past, unit, unitCount);
            TimeMomentRange range = span.GetTimeMomentRange(referenceMoment);
            return range;
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="unitCount"></param>
        /// <param name="referenceMoment"></param>
        /// <returns></returns>
        public static TimeMomentRange Future(this TimeUnits unit, ulong unitCount = 1, DateTimeOffset? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(Timeline.Future, unit, unitCount);
            TimeMomentRange range = span.GetTimeMomentRange(referenceMoment);
            return range;
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="unitCount"></param>
        /// <param name="referenceMoment"></param>
        /// <returns></returns>
        public static TimeMomentRange Previous(this TimeUnits unit, ulong unitCount = 1, DateTimeOffset? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(Timeline.PreviousExcludingCurrent, unit, unitCount);
            TimeMomentRange range = span.GetTimeMomentRange(referenceMoment);
            return range;
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="unitCount"></param>
        /// <param name="referenceMoment"></param>
        /// <returns></returns>
        public static TimeMomentRange Next(this TimeUnits unit, ulong unitCount = 1, DateTimeOffset? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(Timeline.NextExcludingCurrent, unit, unitCount);
            TimeMomentRange range = span.GetTimeMomentRange(referenceMoment);
            return range;
        }


        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangeCurrent(this DateTimeOffset dt, TimeUnits unit)
        {
            return unit.Current(dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangeCurrent(this DateTimeOffset? dt, TimeUnits unit)
        {
            return unit.Current(dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangeToDate(this DateTimeOffset dt, TimeUnits unit)
        {
            return unit.ToDate(dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangeToDate(this DateTimeOffset? dt, TimeUnits unit)
        {
            return unit.ToDate(dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unitCount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangePast(this DateTimeOffset dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Past(unitCount, dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unitCount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangePast(this DateTimeOffset? dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Past(unitCount, dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unitCount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangeFuture(this DateTimeOffset dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Future(unitCount, dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unitCount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangeFuture(this DateTimeOffset? dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Future(unitCount, dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unitCount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangePrevious(this DateTimeOffset dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Previous(unitCount, dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unitCount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangePrevious(this DateTimeOffset? dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Previous(unitCount, dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unitCount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangeNext(this DateTimeOffset dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Next(unitCount, dt);
        }

        /// <summary>
        /// Warning: bug found. dt.Offset may be incorrect as offsets could be different if range crosses daylight saving switch, i.e. October - December, or month of November in the EST USA.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="unitCount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static TimeMomentRange RangeNext(this DateTimeOffset? dt, ulong unitCount, TimeUnits unit)
        {
            return unit.Next(unitCount, dt);
        }

        #endregion TimeMomentRange factory methods

        #region DateRanage factory methods

        public static DateRange Current(this TimeUnits unit, DateTime? referenceMoment)
        {
            var span = new RelativeTimeSpan(Timeline.EntireCurrentOrSpecified, unit);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange ToDate(this TimeUnits unit, DateTime? referenceMoment)
        {
            var span = new RelativeTimeSpan(Timeline.ToDateOrTillSpecified, unit);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange Past(this TimeUnits unit, ulong unitCount, DateTime? referenceMoment)
        {
            var span = new RelativeTimeSpan(Timeline.Past, unit, unitCount);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange Future(this TimeUnits unit, ulong unitCount, DateTime? referenceMoment)
        {
            var span = new RelativeTimeSpan(Timeline.Future, unit, unitCount);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange Previous(this TimeUnits unit, ulong unitCount, DateTime? referenceMoment)
        {
            var span = new RelativeTimeSpan(Timeline.PreviousExcludingCurrent, unit, unitCount);
            DateRange range = span.GetDateTimeRange(referenceMoment);
            return range;
        }

        public static DateRange Next(this TimeUnits unit, ulong unitCount, DateTime? referenceMoment)
        {
            var span = new RelativeTimeSpan(Timeline.NextExcludingCurrent, unit, unitCount);
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