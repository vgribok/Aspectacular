using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aspectacular
{
    public enum SleepResult
    {
        /// <summary>
        /// Entire wait/sleep period elapsed.
        /// </summary>
        Completed, 
        
        /// <summary>
        /// Aborted due to application exiting, or because Threading.ApplicationExiting event was raised manually.
        /// </summary>
        Aborted
    }

    public static class Threading
    {
        /// <summary>
        /// It's raised automatically when application is exiting.
        /// Can be raised manually to abort all sleeping threads.
        /// </summary>
        public static readonly ManualResetEvent ApplicationExiting = new ManualResetEvent(initialState: false);

        static Threading()
        {
            AppDomain.CurrentDomain.DomainUnload += new EventHandler((domainRaw, evt) => ApplicationExiting.Set());
        }

        /// <summary>
        /// A better version of the Thread.Sleep().
        /// Unlike regular sleep, this method returns when application is exiting or AppDomaing is being unloaded.
        /// </summary>
        /// <param name="waitTimeMillisec"></param>
        /// <returns>
        /// Returns Aborted if application is either exiting or ApplicationExiting event was raised.
        /// Returns Completed if entire wait time period has elapsed.
        /// </returns>
        public static SleepResult Sleep(uint waitTimeMillisec)
        {
            if (waitTimeMillisec == 0)
                return SleepResult.Completed;

            if (ApplicationExiting.WaitOne((int)waitTimeMillisec))
                return SleepResult.Aborted;

            return SleepResult.Completed;
        }

        /// <summary>
        /// Waits until async task is completed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public static T Complete<T>(this Task<T> task)
        {
            return task.Result;
        }

        /// <summary>
        /// Waits until async void function is completed.
        /// </summary>
        /// <param name="task"></param>
        public static void Complete(this Task task)
        {
            task.Wait(-1);
        }
    }
}
