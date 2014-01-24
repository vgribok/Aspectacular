using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aspectacular
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Returns DateTime representing beginning of the day, at midnight.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime StartOfDay(this DateTime dt)
        {
            return dt.Date;
        }

        /// <summary>
        /// Returns DateTime representing the last moment of the day specified by dt.
        /// Can be used as an inclusive end date range for a single day.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime EndOfDay(this DateTime dt)
        {
            DateTime start = dt.Date;
            DateTime next = start.AddDays(1);
            DateTime end = new DateTime(next.Ticks - 1);

            return end;
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
