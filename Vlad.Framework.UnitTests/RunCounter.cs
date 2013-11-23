using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.UnitTests
{
    public static class RunCounter
    {
        /// <summary>
        /// Counts how many time certain function was executed in a given time span.
        /// </summary>
        /// <param name="millisecondsToRun"></param>
        /// <param name="funcToTest"></param>
        /// <returns></returns>
        public static long Spin(long millisecondsToRun, Action funcToTest)
        {
            long count = 0, elapsed;

            DateTime start = DateTime.UtcNow;

            do
            {
                funcToTest();
                count++;
                elapsed = (long)(DateTime.UtcNow - start).TotalMilliseconds;
            } while (elapsed < millisecondsToRun);

            Debug.WriteLine("Ran \"{0}\" {1:#,#} times in {2:#,#} milliseconds.", funcToTest, count, millisecondsToRun);

            return count;
        }
    }
}
