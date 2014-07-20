using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aspectacular
{
    /// <summary>
    /// An adapter for turning non-blocking polling into 
    /// either blocking wait or a callback.
    /// </summary>
    /// <remarks>
    /// Polling of queues or monitoring state changes can be difficult: 
    /// from CPU hogging if polling is done in a tight loop, 
    /// to leaking money when polling calls call paid cloud services 
    /// or waste valuable resources, like limited bandwidth.
    /// This adapter will add and grow delays (up to a limit) between poll calls that come back empty.
    /// It can be used either in blocking mode, by calling WaitForPayload() method, 
    /// or made to notify a caller via callback.
    /// </remarks>
    /// <typeparam name="TPollRetVal"></typeparam>
    public class PollPseudoCallback<TPollRetVal>
    {
        public delegate bool PollFunc(out TPollRetVal payload);

        private readonly ManualResetEvent stopSignal = new ManualResetEvent(initialState: true);
        private readonly PollFunc asyncPollFunc;
        private readonly Func<TPollRetVal, bool> processFunc;

        public readonly int MaxPollSleepDelayMillisec;
        public readonly int DelayAfterFirstEmptyPollMillisec;

        private readonly WaitHandle[] abortSignals;
        private readonly TaskScheduler taskScheduler;
        private readonly SynchronizationContext syncContext;

        public PollPseudoCallback(PollFunc asyncPollFunc = null, 
                                  Func<TPollRetVal, bool> processFunc = null,  
                                  int maxPollSleepDelayMillisec = 60 * 1000, 
                                  int delayAfterFirstEmptyPollMillisec = 10,
                                  TaskScheduler scheduler = null,
                                  SynchronizationContext syncContext = null
                                )
        {
            if(maxPollSleepDelayMillisec < 1)
                throw new ArgumentException("maxPollSleepDelayMillisec must be 1 or larger.");
            if(delayAfterFirstEmptyPollMillisec < 1)
                throw new ArgumentException("delayAfterFirstEmptyPollMillisec must be 1 or larger.");

            this.asyncPollFunc = asyncPollFunc;
            this.processFunc = processFunc;
            this.MaxPollSleepDelayMillisec = maxPollSleepDelayMillisec;
            this.DelayAfterFirstEmptyPollMillisec = delayAfterFirstEmptyPollMillisec;
            this.taskScheduler = scheduler ?? TaskScheduler.Current;
            this.syncContext = syncContext ?? SynchronizationContext.Current;

            var stopEvents = new [] { stopSignal, Threading.ApplicationExiting };
            this.abortSignals = stopEvents.Cast<WaitHandle>().ToArray();
        }

        private T ExecuteWithSyncContext<T>(Func<T> @delegate)
        {
            if(this.syncContext == null)
                return @delegate();
#if !DEBUG
            if(this.syncContext == SynchronizationContext.Current)
                return @delegate();
#endif
            T retVal = default(T);
            this.syncContext.Send(delegate { retVal = @delegate(); }, null);
            return retVal;
        }

        private Task<T> StartSchedulerTask<T>(Func<T> @delegate)
        {
            var task = Task.Factory.StartNew(() => this.ExecuteWithSyncContext(@delegate), CancellationToken.None, TaskCreationOptions.DenyChildAttach, this.taskScheduler);
            return task;
        }

        /// <summary>
        /// Executes delegate on using its own sync context and task scheduler.
        /// This simplifies raising events within caller's context.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delegate"></param>
        /// <returns></returns>
        private T SyncExecuteWithSyncContextAndScheduler<T>(Func<T> @delegate)
        {
#if !DEBUG
            if(this.taskScheduler == TaskScheduler.Current)
                return @delegate();
#endif
            Task<T> task = this.StartSchedulerTask(@delegate);
            return task.Result;
        }

        protected virtual bool Poll(out TPollRetVal payload)
        {
            if(this.asyncPollFunc == null)
                throw new InvalidDataException("Poll function must either be supplied to a constructor as a delegate, or Poll() method must be overridden in a subclass.");

            return this.asyncPollFunc(out payload);
        }

        protected virtual bool ProcessAsync(TPollRetVal polledValue)
        {
            if(this.processFunc == null)
                throw new InvalidDataException("ProcessAsync function must either be supplied to a constructor as a delegate, or ProcessAsync() method must be overridden in a subclass.");

            return this.processFunc(polledValue);
        }

        public async void StartSmartPolling()
        {
            if(!this.IsStopSignalled)
                // Already running.
                return;

            await Task.Run(() => this.RunPollLoop());
        }

        private void RunPollLoop()
        {
            this.stopSignal.Reset();
            
            TPollRetVal polledValue;

            while(this.WaitForPayload(out polledValue))
            {
                var polledValTemp = polledValue;
                this.SyncExecuteWithSyncContextAndScheduler(() => this.ProcessAsync(polledValTemp));
            }
        }

        /// <summary>
        /// Blocks until either payload has arrived, or polling is turned off.
        /// Returns true if payload arrived, and false if application is exiting or stop is signaled.
        /// </summary>
        /// <returns></returns>
        public bool WaitForPayload(out TPollRetVal payload)
        {
            this.stopSignal.Reset();
            return this.WaitForPayloadInternal(out payload);
        }

        private bool WaitForPayloadInternal(out TPollRetVal payload)
        {
            payload = default(TPollRetVal);
            int delayMillisec = 0;
            int delayIncrementMillisec = this.DelayAfterFirstEmptyPollMillisec;

            while(WaitHandle.WaitAny(this.abortSignals, delayMillisec) < 0)
            {
                TPollRetVal polledValue = default(TPollRetVal);
                bool hasPayload = this.SyncExecuteWithSyncContextAndScheduler(() => this.Poll(out polledValue));
                payload = polledValue;
                
                if (hasPayload)
                    return true;

                // Poll came back empty.
                this.IncreasePollDelay(ref delayMillisec, ref delayIncrementMillisec);
            }

            return false;
        }

        /// <summary>
        /// Called after poll method came back empty.
        /// Increases delay between subsequent empty poll calls.
        /// </summary>
        /// <param name="delayMillisec"></param>
        /// <param name="delayIncrementMillisec"></param>
        protected virtual void IncreasePollDelay(ref int delayMillisec, ref int delayIncrementMillisec)
        {
            if(delayMillisec >= this.MaxPollSleepDelayMillisec)
                return;

            // Increase delay
            delayMillisec += delayIncrementMillisec;

            if(delayMillisec > this.MaxPollSleepDelayMillisec)
                // Ensure delay does not exceed specified maximum
                delayMillisec = this.MaxPollSleepDelayMillisec;
            else
                delayIncrementMillisec *= 2;
        }

        public bool IsStopSignalled
        {
            get
            {
                bool stopSignalled = this.stopSignal.WaitOne(0);
                bool applicationExiting = Threading.ApplicationExiting.WaitOne(0);
                return stopSignalled || applicationExiting;
            }
        }

        public void StopSmartPolling()
        {
            this.stopSignal.Set();
        }
    }
}
