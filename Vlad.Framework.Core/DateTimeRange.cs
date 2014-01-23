using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aspectacular
{

    /*
     *            Time Unit Start    Reference Moment      Time Unit End
     *  Example:   (Day start)             (Now)              (Day End)
     * ----- Previous -->|<-- Current -------|-------- Current -->|<-- Next -------
     * ----------------------------- Past -->|<-- Future --------------------------
     * 
     */


    [Flags]
    public enum TimeUnits : int
    {
        UtcUnit = 0x10, // Time unit that is better suited for UTC time ranges (like PAST xxx units). Typically, hours, minutes and seconds.
        Seconds = UtcUnit + 1,
        Minutes = UtcUnit + 2,
        Hours = UtcUnit + 3,
        Eternity = UtcUnit + 4, // Open-ended past or future

        LocalTimeUnit = 0x100, // Time units that is better suited for Local (day-based) time ranges.
        Days = LocalTimeUnit + 1,
        Weeks = LocalTimeUnit + 1, 
        Months = LocalTimeUnit + 2, 
        Quarters = LocalTimeUnit + 3, 
        Years = LocalTimeUnit + 4, 
        Decades = LocalTimeUnit + 5, 
        Centuries = LocalTimeUnit + 6,

        //DayOfWeek = 0x1000, // can be used only with the span of 1, and only previous
        //Sunday = LocalTimeSpan + DayOfWeek + 1,
        //Monday = LocalTimeSpan + DayOfWeek + 2,
        //Tuesday = LocalTimeSpan + DayOfWeek + 3,
        //Wednesday = LocalTimeSpan + DayOfWeek + 4,
        //Thursday = LocalTimeSpan + DayOfWeek + 5,
        //Friday = LocalTimeSpan + DayOfWeek + 6,
        //Saturday = LocalTimeSpan + DayOfWeek + 7,
    }

    [Flags]
    public enum TimespanQualifiers : int
    {
        UtcTimespan = 0x10,
        Past = UtcTimespan + 1,
        Future = UtcTimespan + 2,

        LocalTimeSpan = 0x100, 
        AllowsOnlySingle = 0x1000,

        CurrentOrSpecified = LocalTimeSpan + AllowsOnlySingle + 0,
        Previous = LocalTimeSpan + 1,
        PreviousAndCurrent = Previous + CurrentOrSpecified,
        Next = LocalTimeSpan + 2,
        CurrentAndNext = CurrentOrSpecified + Next,

        //PreviousDayOfWeek = Previous + AllowsOnlySingle,
        //NextDayOfWeek = Next + AllowsOnlySingle,
    }


    public enum UtcTimeUnits : int
    {
        Second = TimeUnits.Seconds,
        Minute = TimeUnits.Minutes,
        Hour = TimeUnits.Hours,
    }

    public enum UtcTimespanQualifiers : int
    {
        Past = TimespanQualifiers.Past,
        Future = TimespanQualifiers.Future,
    }


    public enum LocalTimeUnits : int
    {
        Second = TimeUnits.Seconds,
        Minute = TimeUnits.Minutes,
        Hour = TimeUnits.Hours,

        Day = TimeUnits.Days,
        Week = TimeUnits.Weeks,
        Month = TimeUnits.Months,
        Quarter = TimeUnits.Quarters,
        Year = TimeUnits.Years,
        Decade = TimeUnits.Decades,
        Century = TimeUnits.Centuries,
    }
    public enum LocalTimespanQualifiers : int
    {
        Previous = TimespanQualifiers.Previous,
        PreviousAndCurrent = TimespanQualifiers.PreviousAndCurrent,
        Next = TimespanQualifiers.Next,
        CurrentAndNext = TimespanQualifiers.CurrentAndNext,
    }

    /// <summary>
    /// More intuitive time range specification, than start date - end date type of date/time range.
    /// For example, "Previous 3 quarters", "Current week", "Past 48 hours".
    /// </summary>
    public class RelativeTimeSpan
    {
        public readonly bool IsUtcRange;

        private readonly TimeUnits unit;
        private readonly TimespanQualifiers direction;
        public readonly ulong Span;

        /// <summary>
        /// Constructor for Past and Future ranges
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="unit"></param>
        /// <param name="span"></param>
        /// <param name="referenceMoment"></param>
        public RelativeTimeSpan(UtcTimespanQualifiers direction, UtcTimeUnits unit, ulong span = 1)
        {
            if (span == 0)
                throw new ArgumentOutOfRangeException("span value must be 1 or greater.");

            this.IsUtcRange = true;

            this.Span = span;
            this.unit = (TimeUnits)(int)unit;
            this.direction = (TimespanQualifiers)(int)direction;
        }

        /// <summary>
        /// Constructor for Previous and Future ranges
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="unit"></param>
        /// <param name="span"></param>
        /// <param name="referenceMoment"></param>
        public RelativeTimeSpan(LocalTimespanQualifiers direction, LocalTimeUnits unit, ulong span = 1)
        {
            if (span == 0)
                throw new ArgumentOutOfRangeException("span value must be 1 or greater.");

            this.IsUtcRange = false;

            this.Span = span;
            this.unit = (TimeUnits)(int)unit;
            this.direction = (TimespanQualifiers)(int)direction;
        }

        /// <summary>
        /// Constructor for Current range
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="referenceMoment"></param>
        public RelativeTimeSpan(LocalTimeUnits unit)
        {
            this.IsUtcRange = false;

            this.Span = 1;
            this.unit = (TimeUnits)(int)unit;
            this.direction = TimespanQualifiers.CurrentOrSpecified;
        }

        public DateRange GetDateTimeRange(out bool isUtc, DateTime? referenceMoment = null)
        {
            DateTime refMoment = referenceMoment == null ? (this.IsUtcRange ? DateTime.UtcNow : DateTime.Now) : referenceMoment.Value;

            // TODO : Implement range calculation.
            throw new NotImplementedException();
        }

    }

    public static class RelativeTimeSpanExtensions
    {
        public static DateRange Current(this LocalTimeUnits unit, out bool isUtc, DateTime? referenceMoment = null)
        {
            var span = new RelativeTimeSpan(unit);
            DateRange range = span.GetDateTimeRange(out isUtc, referenceMoment);
            return range;
        }

        public static DateTime GoTo(this DateTime dt, Func<DateTime, bool> searchFunc, Func<DateTime, DateTime> stepFunc)
        {
            while (!searchFunc(dt))
                dt = stepFunc(dt);

            return dt;
        }
    }
}
