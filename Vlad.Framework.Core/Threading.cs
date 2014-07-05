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
            AppDomain.CurrentDomain.DomainUnload += (domainRaw, evt) => ApplicationExiting.Set();
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
        /// Throws TimeoutException if timeout exceeded.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="waitTimeoutMillisec">-1 is indefinite timeout</param>
        /// <returns></returns>
        /// <remarks>
        /// Please note that if timeout exceeded, the task is not aborted by this method.
        /// </remarks>
        public static T Complete<T>(this Task<T> task, int waitTimeoutMillisec = -1)
        {
            if (!task.Wait(waitTimeoutMillisec))
                throw new TimeoutException("\"{0}\" timed out after {1:#,#0} milliseconds.".SmartFormat(task, waitTimeoutMillisec));

            return task.Result;
        }

        /// <summary>
        /// Waits until async void function is completed.
        /// Throws TimeoutException if timeout exceeded.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="waitTimeoutMillisec">-1 is indefinite timeout</param>
        /// <remarks>
        /// Please note that if timeout exceeded, the task is not aborted by this method.
        /// </remarks>
        public static void Complete(this Task task, int waitTimeoutMillisec = -1)
        {
            if (!task.Wait(waitTimeoutMillisec))
                throw new TimeoutException("\"{0}\" timed out after {1:#,#0} milliseconds.".SmartFormat(task, waitTimeoutMillisec));
        }

        /// <summary>
        /// Converts multiple WaitHandles to the collection of Task objects.
        /// </summary>
        /// <param name="handles"></param>
        /// <param name="waitTimeoutMillisec"></param>
        /// <returns></returns>
        public static IEnumerable<Task> AsTasks(this IEnumerable<WaitHandle> handles, int waitTimeoutMillisec = -1)
        {
            return handles.Select(handle => handle.AsTask(waitTimeoutMillisec));
        }

        /// <summary>
        /// Converts WaitHandle to Task.
        /// Idea glanced from http://stackoverflow.com/a/18766131.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="waitTimeoutMillisec"></param>
        /// <returns></returns>
        public static Task AsTask(this WaitHandle handle, int waitTimeoutMillisec = -1)
        {
            var tcs = new TaskCompletionSource<object>();
            RegisteredWaitHandle registration = ThreadPool.RegisterWaitForSingleObject(handle, WaitHandleWaiter, tcs, waitTimeoutMillisec, executeOnlyOnce: true);
            //tcs.Task.ContinueWith((hkw, state) => ((RegisteredWaitHandle)state).Unregister(null), registration, TaskScheduler.Default);
            tcs.Task.ContinueWith(_ => registration.Unregister(null), TaskScheduler.Default);
            return tcs.Task;
        }

        private static void WaitHandleWaiter(object state, bool timedOut)
        {
            var localTcs = (TaskCompletionSource<object>)state;
            if (timedOut)
                localTcs.TrySetCanceled();
            else
                localTcs.TrySetResult(null);
        }
    }
}
