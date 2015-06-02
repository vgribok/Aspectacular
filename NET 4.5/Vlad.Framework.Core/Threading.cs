#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aspectacular
{
    public enum SleepResult
    {
        /// <summary>
        ///     Entire wait/sleep period elapsed.
        /// </summary>
        Completed,

        /// <summary>
        ///     Aborted due to application exiting, or because Threading.ApplicationExiting event was raised manually.
        /// </summary>
        Aborted
    }

    public static class Threading
    {
        /// <summary>
        ///     It's raised automatically when application is exiting.
        ///     Can be raised manually to abort all sleeping threads.
        /// </summary>
        public static readonly ManualResetEvent ApplicationExiting = new ManualResetEvent(false);

        static Threading()
        {
            AppDomain.CurrentDomain.DomainUnload += (domainRaw, evt) => ApplicationExiting.Set();
        }

        /// <summary>
        ///     A better version of the Thread.Sleep().
        ///     Unlike regular sleep, this method returns when application is exiting or AppDomaing is being unloaded.
        /// </summary>
        /// <param name="waitTimeMillisec"></param>
        /// <returns>
        ///     Returns Aborted if application is either exiting or ApplicationExiting event was raised.
        ///     Returns Completed if entire wait time period has elapsed.
        /// </returns>
        public static SleepResult Sleep(uint waitTimeMillisec)
        {
            if(waitTimeMillisec == 0)
                return SleepResult.Completed;

            if(ApplicationExiting.WaitOne((int)waitTimeMillisec))
                return SleepResult.Aborted;

            return SleepResult.Completed;
        }

        /// <summary>
        ///     Waits until async task is completed.
        ///     Throws TimeoutException if timeout exceeded.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="waitTimeoutMillisec">-1 is indefinite timeout</param>
        /// <returns></returns>
        /// <remarks>
        ///     Please note that if timeout exceeded, the task is not aborted by this method.
        /// </remarks>
        public static T Complete<T>(this Task<T> task, int waitTimeoutMillisec = -1)
        {
            if(!task.Wait(waitTimeoutMillisec))
                throw new TimeoutException("\"{0}\" timed out after {1:#,#0} milliseconds.".SmartFormat(task, waitTimeoutMillisec));

            return task.Result;
        }

        /// <summary>
        ///     Waits until async void function is completed.
        ///     Throws TimeoutException if timeout exceeded.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="waitTimeoutMillisec">-1 is indefinite timeout</param>
        /// <remarks>
        ///     Please note that if timeout exceeded, the task is not aborted by this method.
        /// </remarks>
        public static void Complete(this Task task, int waitTimeoutMillisec = -1)
        {
            if(!task.Wait(waitTimeoutMillisec))
                throw new TimeoutException("\"{0}\" timed out after {1:#,#0} milliseconds.".SmartFormat(task, waitTimeoutMillisec));
        }

        /// <summary>
        ///     Converts multiple WaitHandles to the collection of Task objects.
        /// </summary>
        /// <param name="handles"></param>
        /// <param name="waitTimeoutMillisec"></param>
        /// <returns></returns>
        public static IEnumerable<Task> AsTasks(this IEnumerable<WaitHandle> handles, int waitTimeoutMillisec = -1)
        {
            return handles.Select(handle => handle.AsTask(waitTimeoutMillisec));
        }

        /// <summary>
        ///     Converts WaitHandle to Task.
        ///     Idea glanced from http://stackoverflow.com/a/18766131.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="waitTimeoutMillisec"></param>
        /// <returns></returns>
        public static Task AsTask(this WaitHandle handle, int waitTimeoutMillisec = -1)
        {
            var tcs = new TaskCompletionSource<object>();
            RegisteredWaitHandle registration = ThreadPool.RegisterWaitForSingleObject(handle, WaitHandleWaiter, tcs, waitTimeoutMillisec, true);
            //tcs.Task.ContinueWith((hkw, state) => ((RegisteredWaitHandle)state).Unregister(null), registration, TaskScheduler.Default);
            tcs.Task.ContinueWith(_ => registration.Unregister(null), TaskScheduler.Default);
            return tcs.Task;
        }

        private static void WaitHandleWaiter(object state, bool timedOut)
        {
            var localTcs = (TaskCompletionSource<object>)state;
            if(timedOut)
                localTcs.TrySetCanceled();
            else
                localTcs.TrySetResult(null);
        }

        /// <summary>
        /// Waits for any task in the collection to complete
        /// </summary>
        /// <param name="tasks">Collection of tasks</param>
        /// <param name="completedTask">Returned first completed task, or null if timeout was reached before any task completed.</param>
        /// <param name="waitTimeoutMillisec">Timeout. Set to -1 for indefinite timeout.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Index of the first completed task, or -1 if timeout was reached before any task timed out.</returns>
        public static int WaitAny(this IEnumerable<Task> tasks, out Task completedTask, int waitTimeoutMillisec = -1, CancellationToken? cancellationToken = null)
        {
            Task[] taskArray = tasks.ToArray();

            int index = Task.WaitAny(taskArray, waitTimeoutMillisec, cancellationToken ?? CancellationToken.None);
            completedTask = index < 0 || index >= taskArray.Length ? null : taskArray[index];

            return index;
        }

        /// <summary>
        /// Waits for any task in the collection to complete.
        /// </summary>
        /// <param name="completedTask">Returns first task that got completed.</param>
        /// <param name="tasks">Collection of tasks</param>
        /// <returns>Index of completed task</returns>
        public static int WaitAny(out Task completedTask, params Task[] tasks)
        {
            return tasks.WaitAny(out completedTask);
        }

        /// <summary>
        ///     Waits for either the task completion, or arrival of the stop signal.
        ///     If stop signal was raised, default(T)/null will be returned.
        ///     Otherwise, task result till be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="waitExitSignal"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static T WaitUntilStopped<T>(this WaitHandle waitExitSignal, Task<T> task)
        {
            Task completedTask;
            int index = WaitAny(out completedTask, waitExitSignal.AsTask(), ApplicationExiting.AsTask(), task);
            return index == 2 ? ((Task<T>)completedTask).Result : default(T);
        }

        /// <summary>
        ///     Returns true if task was completed, false if exit signal was raised.
        /// </summary>
        /// <param name="waitExitSignal"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static bool WaitUntilStopped(this WaitHandle waitExitSignal, Task task)
        {
            Task completedTask;
            int index = WaitAny(out completedTask, waitExitSignal.AsTask(), ApplicationExiting.AsTask(), task);
            return index == 2;
        }

        /// <summary>
        ///     Returns null if stop signal was raised.
        ///     Otherwise returns task that was completed.
        /// </summary>
        /// <param name="waitExitSignal"></param>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static Task WaitUntilStopped(this WaitHandle waitExitSignal, params Task[] tasks)
        {
            Task completedTask;
            WaitAny(out completedTask, tasks);
            return completedTask;
        }


        /// <summary>
        /// Convenience method to execute a function within given SynchronizationContext.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="syncContext"></param>
        /// <param name="delegate"></param>
        /// <returns></returns>
        public static T Execute<T>(this SynchronizationContext syncContext, Func<T> @delegate)
        {
            if (syncContext == null)
                return @delegate();
#if !DEBUG
            if(syncContext == SynchronizationContext.Current)
                return @delegate();
#endif
            T retVal = default(T);
            syncContext.Send(delegate { retVal = @delegate(); }, null);
            return retVal;
        }

        /// <summary>
        /// Convenience method to execute a function within given SynchronizationContext.
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="delegate"></param>
        public static void Execute(this SynchronizationContext syncContext, Action @delegate)
        {
            if(syncContext == null)
            {
                @delegate();
                return;
            }
#if !DEBUG
            if(syncContext == SynchronizationContext.Current)
            {
                @delegate();
                return;
            }
#endif
            syncContext.Send(delegate { @delegate(); }, null);
        }
    }
}