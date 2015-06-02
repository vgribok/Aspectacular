#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Aspectacular.Test
{
    public static class RunCounter
    {
        /// <summary>
        ///     Counts how many time certain function was executed in a given time span.
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
            } while(elapsed < millisecondsToRun);

            Debug.WriteLine("Ran \"{0}\" {1:#,#} times in {2:#,#} milliseconds.", funcToTest, count, millisecondsToRun);

            return count;
        }

        /// <summary>
        ///     Runs a function for given time span, and return average runs per second.
        /// </summary>
        /// <param name="millisecondsToRun"></param>
        /// <param name="funcToTest"></param>
        /// <returns></returns>
        public static long SpinPerSec(long millisecondsToRun, Action funcToTest)
        {
            long count = Spin(millisecondsToRun, funcToTest);
            long runsPerSec = count/(millisecondsToRun/1000);
            return runsPerSec;
        }

        /// <summary>
        ///     Runs given function in parallel on multiple tasks.
        ///     Number of tasks spawned matches number of logical processors.
        /// </summary>
        /// <param name="millisecondsToRun"></param>
        /// <param name="funcToTest"></param>
        /// <returns></returns>
        public static long SpinParallel(long millisecondsToRun, Action funcToTest)
        {
            Task<long>[] tasks = new Task<long>[Environment.ProcessorCount];

            for(int i = 0; i < Environment.ProcessorCount; i++)
            {
                tasks[i] = new Task<long>(() => Spin(millisecondsToRun, funcToTest));
            }

            DateTime start = DateTime.UtcNow;

            tasks.ForEach(task => task.Start());
// ReSharper disable once CoVariantArrayConversion
            Task.WaitAll(tasks);

            long ranMillisec = (long)(DateTime.UtcNow - start).TotalMilliseconds;
            Debug.WriteLine("Parallel call counting run time mismatch is {0:#,#} milliseconds.", ranMillisec - millisecondsToRun);

            long[] runCounts = tasks.Select(thread => thread.Result).ToArray();
            long total = runCounts.Sum();
            return total;
        }

        /// <summary>
        ///     Runs a function for given time span, and return average runs per second.
        ///     Number of tasks spawned matches number of logical processors.
        /// </summary>
        /// <param name="millisecondsToRun"></param>
        /// <param name="funcToTest"></param>
        /// <returns></returns>
        public static long SpinParallelPerSec(long millisecondsToRun, Action funcToTest)
        {
            long count = SpinParallel(millisecondsToRun, funcToTest);
            long runsPerSec = count/(millisecondsToRun/1000);
            return runsPerSec;
        }
    }
}