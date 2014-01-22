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

        LocalTimeUnit = 0x100, // Time units that is better suited for Local (day-based) time ranges.
        Days = LocalTimeUnit + 1,
        Weeks = LocalTimeUnit + 1, 
        Months = LocalTimeUnit + 2, 
        Quarters = LocalTimeUnit + 3, 
        Years = LocalTimeUnit + 4, 
        Decades = LocalTimeUnit + 5, 
        Centuries = LocalTimeUnit + 6,

        //DayOfWeek = 0x1000, // can be used only with the span of 1, and only previous
        //Sunday = LocalTimeUnit + DayOfWeek + 1,
        //Monday = LocalTimeUnit + DayOfWeek + 2,
        //Tuesday = LocalTimeUnit + DayOfWeek + 3,
        //Wednesday = LocalTimeUnit + DayOfWeek + 4,
        //Thursday = LocalTimeUnit + DayOfWeek + 5,
        //Friday = LocalTimeUnit + DayOfWeek + 6,
        //Saturday = LocalTimeUnit + DayOfWeek + 7,
    }

    [Flags]
    public enum TimespanQualifiers : int
    {
        UtcTimespan = 0x100,
        Past = UtcTimespan + 1,
        Future = UtcTimespan + 2,

        LocalTimeUnit = 0x100, 
        AllowsOnlySingle = 0x1000,

        CurrentOrSpecified = LocalTimeUnit + AllowsOnlySingle + 0,
        Previous = LocalTimeUnit + 1,
        PreviousAndCurrent = Previous + CurrentOrSpecified,
        Next = LocalTimeUnit + 2,
        CurrentAndNext = CurrentOrSpecified + Next,

        //PreviousDayOfWeek = Previous + AllowsOnlySingle,
        //NextDayOfWeek = Next + AllowsOnlySingle,
    }


    public enum UtcTimeUnits : int
    {
        Seconds = TimeUnits.Seconds,
        Minutes = TimeUnits.Minutes,
        Hours = TimeUnits.Hours,
    }

    public enum UtcTimespanQualifiers : int
    {
        Past = TimespanQualifiers.Past,
        Future = TimespanQualifiers.Future,
    }


    public enum LocalTimeUnits : int
    {
        Seconds = TimeUnits.Seconds,
        Minutes = TimeUnits.Minutes,
        Hours = TimeUnits.Hours,

        Days = TimeUnits.Days,
        Weeks = TimeUnits.Weeks,
        Months = TimeUnits.Months,
        Quarters = TimeUnits.Quarters,
        Years = TimeUnits.Years,
        Decades = TimeUnits.Decades,
        Centuries = TimeUnits.Centuries,
    }
    public enum LocalTimespanQualifiers : int
    {
        Previous = TimespanQualifiers.Previous,
        PreviousAndCurrent = TimespanQualifiers.PreviousAndCurrent,
        Next = TimespanQualifiers.Next,
        CurrentAndNext = TimespanQualifiers.CurrentAndNext,
    }

    public class RelativeTimeRange
    {
        public readonly bool IsUtcRange;

        private readonly TimeUnits unit;
        private readonly TimespanQualifiers direction;
        public readonly ulong Span;

        private readonly DateTime? referenceMoment;

        /// <summary>
        /// Constructor for Past and Future ranges
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="unit"></param>
        /// <param name="span"></param>
        /// <param name="referenceMoment"></param>
        public RelativeTimeRange(UtcTimespanQualifiers direction, UtcTimeUnits unit, ulong span = 1, DateTime? referenceMoment = null)
        {
            if (span == 0)
                throw new ArgumentOutOfRangeException("span value must be 1 or greater.");

            this.IsUtcRange = true;

            this.referenceMoment = referenceMoment;
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
        public RelativeTimeRange(LocalTimespanQualifiers direction, LocalTimeUnits unit, ulong span = 1, DateTime? referenceMoment = null)
        {
            if (span == 0)
                throw new ArgumentOutOfRangeException("span value must be 1 or greater.");

            this.IsUtcRange = false;

            this.referenceMoment = referenceMoment;
            this.Span = span;
            this.unit = (TimeUnits)(int)unit;
            this.direction = (TimespanQualifiers)(int)direction;
        }

        /// <summary>
        /// Constructor for Current range
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="referenceMoment"></param>
        public RelativeTimeRange(LocalTimeUnits unit, DateTime? referenceMoment = null)
        {
            this.IsUtcRange = false;

            this.referenceMoment = referenceMoment;
            this.Span = 1;
            this.unit = (TimeUnits)(int)unit;
            this.direction = TimespanQualifiers.CurrentOrSpecified;
        }

        public DateRange GetDateTimeRange(out bool isUtc)
        {
            // TODO : Implement range calculation.
            throw new NotImplementedException();
        }

        private DateTime GetReferenceTime()
        {
            if (this.referenceMoment == null)
                return this.IsUtcRange ? DateTime.UtcNow : DateTime.Now;

            return this.referenceMoment.Value;
        }
    }
}
