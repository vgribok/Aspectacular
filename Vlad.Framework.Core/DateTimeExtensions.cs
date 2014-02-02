using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Aspectacular
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Returns quarter number 1..4
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int Quarter(this DateTime dt)
        {
            return (dt.Month - 1) / 3 + 1;
        }

        /// <summary>
        /// Returns ISO-8601 week number in the year.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="whatIsFirstWeek">Tells whether the first week can be a) any incomplete week, b) a week with 4 days or more, or c) full week only.</param>
        /// <returns></returns>
        public static int WeekOfYear(this DateTime dt, CalendarWeekRule whatIsFirstWeek = CalendarWeekRule.FirstFourDayWeek)
        {
            int week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dt, whatIsFirstWeek, CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
            return week;
        }

        /// <summary>
        /// DateTime loop functional implementation.
        /// Continues calling stepFunc while searchFunc keeps returning true:
        ///     while (!searchFunc(dt))
        ///         dt = stepFunc(dt);
        ///     return dt;
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="stopCondFunc"></param>
        /// <param name="stepFunc"></param>
        /// <returns></returns>
        public static DateTime GoTo(this DateTime dt, Func<DateTime, bool> stopCondFunc, Func<DateTime, DateTime> stepFunc)
        {
            while (!stopCondFunc(dt))
                dt = stepFunc(dt);

            return dt;
        }
    }
}
