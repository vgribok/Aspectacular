#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aspectacular
{
    /// <summary>
    ///     An adapter for turning non-blocking polling into
    ///     either blocking wait or a callback.
    /// </summary>
    /// <remarks>
    ///     Polling of queues or monitoring state changes can be difficult:
    ///     from CPU hogging if polling is done in a tight loop,
    ///     to leaking money when polling calls call paid cloud services
    ///     or waste valuable resources, like limited bandwidth.
    ///     This adapter will add and grow delays (up to a limit) between poll calls that come back empty.
    ///     It can be used either in blocking mode, by calling WaitForPayload() method,
    ///     or made to notify a caller via callback.
    ///     User WaitForPayload() or StartNotificationLoop() methods to run the polling loop.
    /// </remarks>
    /// <typeparam name="TPollRetVal"></typeparam>
    public class BlockingPoll<TPollRetVal>
    {
        public delegate bool PollFunc(out TPollRetVal payload);

        private readonly ManualResetEvent stopSignal = new ManualResetEvent(true);
        private readonly PollFunc asyncPollFunc;

        public readonly int MaxPollSleepDelayMillisec;
        public readonly int DelayAfterFirstEmptyPollMillisec;

        private readonly WaitHandle[] abortSignals;

        /// <summary>
        ///     Number of poll calls that came up empty so far.
        /// </summary>
        public ulong EmptyPollCallCount { get; private set; }

        /// <summary>
        ///     Number of poll calls that returned payload.
        ///     Useful only with
        /// </summary>
        public ulong PollCallCountWithPayload { get; private set; }

        /// <summary>
        ///     Initializes poll-to-callback adapter class.
        /// </summary>
        /// <param name="asyncPollFunc">
        ///     Optional delegate implementing polling. If not specified, Poll() method must be overridden in the subclass.
        ///     Callback delegate must return true if payload was acquired, and false if poll method came back empty.
        /// </param>
        /// <param name="maxPollSleepDelayMillisec">
        ///     Maximum delay between poll attempts that come back empty.
        ///     Delays starts with 0 and is increased up to this value if poll calls keep come back empty.
        /// </param>
        /// <param name="delayAfterFirstEmptyPollMillisec">Delay after the first empty poll call following non-empty poll call.</param>
        public BlockingPoll(PollFunc asyncPollFunc = null,
            int maxPollSleepDelayMillisec = 60*1000,
            int delayAfterFirstEmptyPollMillisec = 10
            )
        {
            if(maxPollSleepDelayMillisec < 1)
                throw new ArgumentException("maxPollSleepDelayMillisec must be 1 or larger.");
            if(delayAfterFirstEmptyPollMillisec < 1)
                throw new ArgumentException("delayAfterFirstEmptyPollMillisec must be 1 or larger.");

            this.asyncPollFunc = asyncPollFunc;
            this.MaxPollSleepDelayMillisec = maxPollSleepDelayMillisec;
            this.DelayAfterFirstEmptyPollMillisec = delayAfterFirstEmptyPollMillisec;

            var stopEvents = new[] {stopSignal, Threading.ApplicationExiting};
            this.abortSignals = stopEvents.Cast<WaitHandle>().ToArray();

            this.PollCallCountWithPayload = 0;
        }

        /// <summary>
        ///     Returns true if payload was acquired. False if poll call came back empty.
        ///     Can be overridden in subclasses.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected virtual bool Poll(out TPollRetVal payload)
        {
            if(this.asyncPollFunc == null)
                throw new InvalidDataException("Poll function must either be supplied to a constructor as a delegate, or Poll() method must be overridden in a subclass.");

            return this.asyncPollFunc(out payload);
        }

        /// <summary>
        ///     A callback that processes payload inside the non-blocking poll loop started by StartNotificationLoop() method.
        ///     Payload parameter value is non-default(TPollRetVal).
        ///     Can be overridden in subclasses, if StartNotificationLoop() method's payloadProcessCallback parameter value is not
        ///     specified.
        /// </summary>
        /// <param name="payload"></param>
        protected virtual void Process(TPollRetVal payload)
        {
            throw new NotImplementedException("Must be overridden in a subclass.");
        }

        /// <summary>
        ///     Launches polling loop that invokes process callback when payload arrives.
        ///     This is an alternative to using blocking WaitForPayload() method.
        /// </summary>
        /// <param name="payloadProcessCallback">
        ///     Optional payload processing delegate. If null, Process() method must be overridden
        ///     in a subclass.
        /// </param>
        public async void StartNotificationLoop(Action<TPollRetVal> payloadProcessCallback = null)
        {
            if(!this.IsStopped)
                throw new InvalidOperationException("Polling loop is already running. Call Stop() before calling this method.");

            SynchronizationContext syncContext = SynchronizationContext.Current;
            await Task.Run(() => this.RunPollLoop(syncContext, payloadProcessCallback ?? this.Process));
        }

        private void RunPollLoop(SynchronizationContext syncContext, Action<TPollRetVal> processFunc)
        {
            this.stopSignal.Reset();
            this.EmptyPollCallCount = 0;

            TPollRetVal polledValue;

            while(this.WaitForPayloadInternal(out polledValue, syncContext))
            {
                TPollRetVal polledValTemp = polledValue;
                syncContext.Execute(() => processFunc(polledValTemp));
            }
        }

        /// <summary>
        ///     Blocks until either payload arrives, or polling is terminated.
        ///     Returns true if payload arrived, and false if application is exiting or stop is signaled.
        /// </summary>
        /// <returns>
        ///     True if payload was acquired, and false if polling loop was terminated with Stop() method or by application
        ///     exiting.
        /// </returns>
        public bool WaitForPayload(out TPollRetVal payload)
        {
            if(!this.IsStopped)
                throw new InvalidOperationException("Polling loop is already running. Call Stop() before calling this method.");

            this.stopSignal.Reset();
            this.EmptyPollCallCount = 0;
            return this.WaitForPayloadInternal(out payload, syncCtx: null);
        }

        private bool WaitForPayloadInternal(out TPollRetVal payload, SynchronizationContext syncCtx)
        {
            payload = default(TPollRetVal);
            int delayMillisec = 0;
            int delayIncrementMillisec = this.DelayAfterFirstEmptyPollMillisec;

            while(WaitHandle.WaitAny(this.abortSignals, delayMillisec) < 0)
            {
                TPollRetVal polledValue = default(TPollRetVal);
                bool hasPayload = syncCtx.Execute(() => this.Poll(out polledValue));
                payload = polledValue;

                if(hasPayload)
                {
                    this.PollCallCountWithPayload++;
                    return true;
                }
                this.EmptyPollCallCount++;

                // Poll came back empty.
                this.IncreasePollDelay(ref delayMillisec, ref delayIncrementMillisec);
            }

            return false;
        }

        /// <summary>
        ///     Called after poll method came back empty.
        ///     Increases delay between subsequent empty poll calls.
        ///     May be overridden in subclasses to provide different delay adjustment logic.
        /// </summary>
        /// <param name="delayMillisec">Current delay to be changed, between empty poll call attempts</param>
        /// <param name="delayIncrementMillisec">Current delay increment to be changed if necessary.</param>
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

        /// <summary>
        ///     Returns true if neither WaitForPayload(), nor StartNotificationLoop() methods are running.
        /// </summary>
        public bool IsStopped
        {
            get
            {
                bool stopSignalled = this.stopSignal.WaitOne(0);
                bool applicationExiting = Threading.ApplicationExiting.WaitOne(0);
                return stopSignalled || applicationExiting;
            }
        }

        /// <summary>
        ///     Either stops WaitForPayload() or terminates polling loop started with StartNotificationLoop()
        ///     that generates callbacks on payload arrival.
        /// </summary>
        public void Stop()
        {
            this.stopSignal.Set();
        }
    }
}