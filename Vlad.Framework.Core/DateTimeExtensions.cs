#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Globalization;

namespace Aspectacular
{
    /// <summary>
    ///     Contains utility & convenience DateTime methods
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        ///     Returns quarter number 1..4
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int Quarter(this DateTime dt)
        {
            return (dt.Month - 1)/3 + 1;
        }

        /// <summary>
        ///     Returns quarter number 1..4
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int Quarter(this DateTimeOffset dt)
        {
            return (dt.Month - 1)/3 + 1;
        }

        /// <summary>
        ///     Returns ISO-8601 week number in the year.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="whatIsFirstWeek">
        ///     Tells whether the first week can be a) any incomplete week, b) a week with 4 days or
        ///     more, or c) full week only.
        /// </param>
        /// <returns></returns>
        public static int WeekOfYear(this DateTime dt, CalendarWeekRule whatIsFirstWeek = CalendarWeekRule.FirstFourDayWeek)
        {
            int week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dt, whatIsFirstWeek, CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
            return week;
        }

        /// <summary>
        ///     Returns ISO-8601 week number in the year.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="whatIsFirstWeek">
        ///     Tells whether the first week can be a) any incomplete week, b) a week with 4 days or
        ///     more, or c) full week only.
        /// </param>
        /// <returns></returns>
        public static int WeekOfYear(this DateTimeOffset dt, CalendarWeekRule whatIsFirstWeek = CalendarWeekRule.FirstFourDayWeek)
        {
            int week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dt.DateTime, whatIsFirstWeek, CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
            return week;
        }

        /// <summary>
        ///     DateTime loop functional implementation.
        ///     Continues calling stepFunc while searchFunc keeps returning true:
        ///     while (!searchFunc(dt))
        ///     dt = stepFunc(dt);
        ///     return dt;
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="stopCondFunc"></param>
        /// <param name="stepFunc"></param>
        /// <returns></returns>
        public static DateTime GoTo(this DateTime dt, Func<DateTime, bool> stopCondFunc, Func<DateTime, DateTime> stepFunc)
        {
            while(!stopCondFunc(dt))
                dt = stepFunc(dt);

            return dt;
        }

        /// <summary>
        ///     DateTimeOffset loop functional implementation.
        ///     Continues calling stepFunc while searchFunc keeps returning true:
        ///     while (!searchFunc(dt))
        ///     dt = stepFunc(dt);
        ///     return dt;
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="stopCondFunc"></param>
        /// <param name="stepFunc"></param>
        /// <returns></returns>
        public static DateTimeOffset GoTo(this DateTimeOffset dt, Func<DateTimeOffset, bool> stopCondFunc, Func<DateTimeOffset, DateTimeOffset> stepFunc)
        {
            while(!stopCondFunc(dt))
                dt = stepFunc(dt);

            return dt;
        }

        /// <summary>
        ///     Subtracts 1 tick from the given time.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime PreviousMoment(this DateTime dt)
        {
            return new DateTime(dt.Ticks - 1, dt.Kind);
        }

        /// <summary>
        ///     Subtracts 1 tick from the given time.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTimeOffset PreviousMoment(this DateTimeOffset dt)
        {
            return new DateTimeOffset(dt.DateTime.PreviousMoment(), dt.Offset);
        }

        public static DateTime ToDateTime(this DateTimeOffset dto, DateTimeKind kind)
        {
            DateTime dt = new DateTime(dto.DateTime.Ticks, kind);
            return dt;
        }

        /// <summary>
        ///     Returns true if DateTimeOffset represents UTC time.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public static bool IsUtc(this DateTimeOffset dto)
        {
            return dto.Offset.Ticks == 0;
        }

        #region Sortable DateTime Integer Conversion

        /// <summary>
        ///     Converts date part of the date time
        ///     to integer in the YYYYMMDD format.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>Integer in the YYYYMMDD format</returns>
        public static int ToSortableIntDate(this DateTime dt)
        {
            int retVal = (dt.Year*100 + dt.Month)*100 + dt.Day;
            return retVal;
        }

        /// <summary>
        ///     Returns integer in the format of HHmmss or HHmmssFFF.
        ///     Optional "FFF" is milliseconds.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="includeMilliseconds"></param>
        /// <returns></returns>
        public static long ToSortableLongTime(this DateTime dt, bool includeMilliseconds = false)
        {
            long retVal = (dt.Hour*100 + dt.Minute)*100 + dt.Second;

            if(includeMilliseconds)
                retVal = retVal*1000 + dt.Millisecond;

            return retVal;
        }

        /// <summary>
        ///     Returns integer in the format of HHmmss or HHmmssFFF.
        ///     Optional "FFF" is milliseconds.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="includeMilliseconds"></param>
        /// <returns></returns>
        public static int ToSortableIntTime(this DateTime dt, bool includeMilliseconds = false)
        {
            long sortableTime = ToSortableLongTime(dt, includeMilliseconds);
            if(sortableTime > int.MaxValue)
                throw new Exception("{0:#,#0} value exceeds maximum integer value".SmartFormat(sortableTime));

            return (int)sortableTime;
        }

        /// <summary>
        ///     Returns integer in the format of YYYYMMDDHHmmss or similar with fractional seconds.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="includeMilliseconds"></param>
        /// <returns></returns>
        public static long ToSortableLongDateTime(this DateTime dt, bool includeMilliseconds = false)
        {
            long multiplier = includeMilliseconds ? 1000000000 : 1000000;
            long result = ToSortableIntDate(dt)*multiplier + ToSortableLongTime(dt, includeMilliseconds);

            return result;
        }

        /// <summary>
        ///     Returns DateTime created from an integer in the YYYYMMDD, YYYYMMDDHHmmss, or YYYYMMDDHHmmssFFF format.
        /// </summary>
        /// <param name="sortableDateTime"></param>
        /// <param name="dtKind"></param>
        /// <returns></returns>
        public static DateTime FromSortableIntDateTime(this long sortableDateTime, DateTimeKind dtKind = DateTimeKind.Unspecified)
        {
            const byte digitsInDateOnly = 4 + 2 + 2;
            byte totalDigits = (byte)((int)Math.Log10(sortableDateTime) + 1);
            byte timePartDigits = (byte)(totalDigits - digitsInDateOnly);
            int timePartOrder = (int)10.Pow(timePartDigits);

            int sortableDate = (int)(sortableDateTime/timePartOrder); // YYYYMMDD
            int day = sortableDate%100;
            sortableDate /= 100;
            int month = sortableDate%100;
            sortableDate /= 100;
            int year = sortableDate;

            DateTime dt = new DateTime(year, month, day, 0, 0, 0, dtKind);

            long sortableTimePart = sortableDateTime%timePartOrder; // 0, YYYYMMDDHHmmss, or YYYYMMDDHHmmssFFF
            if(sortableTimePart > 0)
                dt = sortableTimePart.FromSortableIntTime(dt);

            return dt;
        }

        /// <summary>
        ///     Returns DateTime created from an integer in the YYYYMMDD, YYYYMMDDHHmmss, or YYYYMMDDHHmmssFFF format.
        /// </summary>
        /// <param name="sortableDateTime"></param>
        /// <param name="dtKind"></param>
        /// <returns></returns>
        public static DateTime FromSortableIntDateTime(this int sortableDateTime, DateTimeKind dtKind = DateTimeKind.Unspecified)
        {
            return FromSortableIntDateTime((long)sortableDateTime, dtKind);
        }

        /// <summary>
        ///     Returns DateTime created from an integer in the YYYYMMDD, YYYYMMDDHHmmss, or YYYYMMDDHHmmssFFF format.
        /// </summary>
        /// <param name="sortableDateTime"></param>
        /// <param name="dtKind"></param>
        /// <returns></returns>
        public static DateTime? FromSortableIntDateTime(this string sortableDateTime, DateTimeKind dtKind = DateTimeKind.Unspecified)
        {
            long? sortableVal = sortableDateTime.ParseLong();
            if(sortableVal == null)
                return null;

            return sortableVal.Value.FromSortableIntDateTime(dtKind);
        }

        /// <summary>
        ///     Returns time value via DateTime result
        ///     created from integer in the HHmmss or HHmmssFFF format.
        ///     Optional "FFF" is milliseconds.
        /// </summary>
        /// <param name="sortableDateTime"></param>
        /// <param name="datePart">Date to which time will be appended.</param>
        /// <returns></returns>
        public static DateTime FromSortableIntTime(this long sortableDateTime, DateTime datePart = default(DateTime))
        {
            const int noMilliseconds = 1000000;
            bool hasMilliseconds = sortableDateTime > noMilliseconds*10;

            int milliseconds = 0;

            if(hasMilliseconds)
            {
                milliseconds = (int)(sortableDateTime%1000);
                sortableDateTime /= 1000;
            }

            int seconds = (int)sortableDateTime%100;
            sortableDateTime /= 100;

            int minutes = (int)sortableDateTime%100;
            sortableDateTime /= 100;

            int hours = (int)sortableDateTime;

            DateTime dt = new DateTime(datePart.Year, datePart.Month, datePart.Day, hours, minutes, seconds, milliseconds, datePart.Kind);
            return dt;
        }

        /// <summary>
        ///     Returns time value via DateTime result
        ///     created from integer in the HHmmss or HHmmssFFF format.
        ///     Optional "FFF" is milliseconds.
        /// </summary>
        /// <param name="sortableDateTime"></param>
        /// <param name="datePart">Date to which time will be appended.</param>
        /// <returns></returns>
        public static DateTime FromSortableIntTime(this int sortableDateTime, DateTime datePart = default(DateTime))
        {
            return FromSortableIntTime((long)sortableDateTime, datePart);
        }

        /// <summary>
        ///     Returns time value via DateTime result
        ///     created from integer in the HHmmss or HHmmssFFF format.
        ///     Optional "FFF" is milliseconds.
        /// </summary>
        /// <param name="sortableDateTime"></param>
        /// <param name="datePart">Date to which time will be appended.</param>
        /// <returns></returns>
        public static DateTime? FromSortableIntTime(this string sortableDateTime, DateTime datePart = default(DateTime))
        {
            long? sortableVal = sortableDateTime.ParseLong();
            if(sortableVal == null)
                return null;

            return sortableVal.Value.FromSortableIntTime(datePart);
        }

        #endregion Sortable DateTime Integer Conversion
    }

    public static class MathExtensions
    {
        /// <summary>
        ///     Fast calculation of x power of n for integers.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="power"></param>
        /// <returns></returns>
        public static long Pow(this int x, byte power)
        {
            long result = 1, ipower = power;
            ipower.Repeat(_ => result *= x);
            return result;
        }
    }
}