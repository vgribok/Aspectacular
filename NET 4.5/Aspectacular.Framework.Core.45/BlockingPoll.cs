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
    ///     either blocking wait or a callback, enabling pub/sub pattern instead of having to poll.
    ///     User this class if null can be a valid payload.
    ///     If valid payload cannot be null, use BlockingObjectPoll class instead.
    /// </summary>
    /// <remarks>
    ///     Polling of queues or monitoring state changes can be difficult:
    ///     from CPU hogging if polling is done in a tight loop,
    ///     to leaking money when polling calls paid cloud services
    ///     or wasting valuable resources, like limited bandwidth.
    ///     This adapter will do polling with growing delays between poll calls (up to a limit) that come back empty.
    ///     It can be used either in blocking mode, by calling WaitForPayload() method, or made to notify a caller via callback.
    ///     User WaitForPayload() or RegisterCallbackHandler() methods to run the polling loop.
    /// </remarks>
    /// <typeparam name="TPollRetVal">Payload that may have null as valid payload.</typeparam>
    public class BlockingPoll<TPollRetVal> : IDisposable
    {
        #region Fields

        private readonly ManualResetEvent stopSignal = new ManualResetEvent(true);

        /// <summary>
        ///     Poll delegate, returning combination of Boolean telling whether payload was retrieved, and payload itself.
        /// </summary>
        /// <returns></returns>
        private readonly Func<Tuple<bool, TPollRetVal>> asyncPollFunc;

        /// <summary>
        /// Maximum delay time, in milliseconds, between subsequent polling hits.
        /// </summary>
        public readonly int MaxPollSleepDelayMillisec;
        
        /// <summary>
        /// Delay time, in milliseconds, after the first polling hit that came back empty.
        /// </summary>
        public readonly int DelayAfterFirstEmptyPollMillisec;

        private readonly WaitHandle[] abortSignals;

        #endregion Fields

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
        public BlockingPoll(Func<Tuple<bool, TPollRetVal>> asyncPollFunc = null,
            int maxPollSleepDelayMillisec = 60 * 1000,
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
        ///     Launches polling loop that invokes process callback when payload arrives.
        ///     This is an alternative to using blocking WaitForPayload() method.
        /// </summary>
        /// <param name="payloadProcessCallback">
        ///     Optional payload processing delegate. If null, Process() method must be overridden
        ///     in a subclass.
        /// </param>
        /// <remarks>
        ///     This method starts polling thread, on which both polling function and payload processing functions are called.
        ///     This means that next attempt to dequeue messages won't occur until payload processing callback function is done.
        ///     Payload processing callback may start its own thread(s) to process messages asynchronously and quickly return
        ///     control to the polling thread.
        /// </remarks>
#if NET40
        public Task Subscribe(Action<TPollRetVal> payloadProcessCallback = null)
#else
        public async void Subscribe(Action<TPollRetVal> payloadProcessCallback = null)
#endif
        {
            if(!this.IsStopped)
                throw new InvalidOperationException("Polling loop is already running. Call Stop() before calling this method.");

            SynchronizationContext syncContext = SynchronizationContext.Current;
            Task task = Task.Factory.StartNew(() => this.RunPollLoop(syncContext, payloadProcessCallback ?? this.Process));
#if NET40
            return task;
#else
            await task;
#endif
        }

        /// <summary>
        ///     Blocks until either payload arrives, or polling is terminated.
        ///     Returns true if payload arrived, and false if application is exiting or stop is signaled.
        /// </summary>
        /// <returns>
        ///     True if payload was acquired, and false if polling loop was terminated with Stop() method or by application
        ///     exiting.
        /// </returns>
        public Tuple<bool, TPollRetVal> WaitForPayload()
        {
            if(!this.IsStopped)
                throw new InvalidOperationException("Polling loop is already running. Call Stop() before calling this method.");

            this.stopSignal.Reset();
            this.EmptyPollCallCount = 0;

            try
            {
                TPollRetVal payload;
                bool success = this.WaitForPayloadInternal(out payload, syncCtx: null);
                return new Tuple<bool, TPollRetVal>(success, payload);
            }
            finally
            {
                this.Stop();
            }
        }

        /// <summary>
        ///     Either stops WaitForPayload() or terminates polling loop started with RegisterCallbackHandler()
        ///     that generates callbacks on payload arrival.
        /// </summary>
        public void Stop()
        {
            this.stopSignal.Set();
        }

        #region Properties

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
        ///     Returns true if neither WaitForPayload(), nor RegisterCallbackHandler() methods are running.
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

        #endregion Properties

        #region Virtual Methods

        /// <summary>
        ///     Returns true if payload was acquired. False if poll call came back empty.
        ///     Can be overridden in subclasses.
        /// </summary>
        /// <returns></returns>
        protected virtual Tuple<bool, TPollRetVal> Poll()
        {
            if(this.asyncPollFunc == null)
                throw new InvalidDataException("Poll function must either be supplied to a constructor as a delegate, or Poll() method must be overridden in a subclass.");

            return this.asyncPollFunc();
        }

        /// <summary>
        ///     A callback that processes payload inside the non-blocking poll loop started by RegisterCallbackHandler() method.
        ///     Payload parameter value is non-default(TPollRetVal).
        ///     Can be overridden in subclasses, if RegisterCallbackHandler() method's payloadProcessCallback parameter value is not
        ///     specified.
        /// </summary>
        /// <param name="payload"></param>
        protected virtual void Process(TPollRetVal payload)
        {
            throw new NotImplementedException("Must be overridden in a subclass.");
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

        #endregion Virtual Methods

        #region Utility Methods

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

        private bool WaitForPayloadInternal(out TPollRetVal payload, SynchronizationContext syncCtx)
        {
            payload = default(TPollRetVal);
            int delayMillisec = 0;
            int delayIncrementMillisec = this.DelayAfterFirstEmptyPollMillisec;

            while(this.SleepAfterEmptyPollCall(delayMillisec))
            {
                Tuple<bool, TPollRetVal> retVal = syncCtx.Execute(() => this.Poll());
                payload = retVal.Item2;

                if(retVal.Item1)
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

        private bool SleepAfterEmptyPollCall(int delayMillisec)
        {
            int index = WaitHandle.WaitAny(this.abortSignals, delayMillisec);
            bool canContinue = index == WaitHandle.WaitTimeout;
            return canContinue;
        }

        #endregion Utility Methods

        /// <summary>
        ///     Implements IDisposable and stops polling.
        /// </summary>
        public void Dispose()
        {
            this.Stop();
        }
    }

    /// <summary>
    ///     An adapter for turning non-blocking polling into
    ///     either blocking wait or a callback.
    ///     This version is a simplified polling adapter class whose polling payload is non-null when present.
    /// </summary>
    /// <remarks>
    ///     Polling of queues or monitoring state changes can be difficult:
    ///     from CPU hogging if polling is done in a tight loop,
    ///     to leaking money when polling calls call paid cloud services
    ///     or waste valuable resources, like limited bandwidth.
    ///     This adapter will add and grow delays (up to a limit) between poll calls that come back empty.
    ///     It can be used either in blocking mode, by calling WaitForPayload() method,
    ///     or made to notify a caller via callback.
    ///     User WaitForPayload() or RegisterCallbackHandler() methods to run the polling loop.
    /// </remarks>
    /// <typeparam name="TNonNullablePayload">Payload type that which never has null as valid payload.</typeparam>
    public class BlockingObjectPoll<TNonNullablePayload> : BlockingPoll<TNonNullablePayload>
        where TNonNullablePayload : class
    {
        private readonly Func<TNonNullablePayload> asyncPollFunc;

        /// <summary>
        ///     Initializes poll-to-callback adapter class where null poll return means data was not acquired.
        /// </summary>
        /// <param name="asyncPollFunc">
        ///     Optional delegate implementing polling. If not specified, Poll() method must be overridden in the subclass.
        ///     Callback delegate must return non-null if payload was acquired, and null if poll method came back empty.
        /// </param>
        /// <param name="maxPollSleepDelayMillisec">
        ///     Maximum delay between poll attempts that come back empty.
        ///     Delays starts with 0 and is increased up to this value if poll calls keep come back empty.
        /// </param>
        /// <param name="delayAfterFirstEmptyPollMillisec">Delay after the first empty poll call following non-empty poll call.</param>
        public BlockingObjectPoll(Func<TNonNullablePayload> asyncPollFunc = null,
            int maxPollSleepDelayMillisec = 60 * 1000,
            int delayAfterFirstEmptyPollMillisec = 10)
            : base(null, maxPollSleepDelayMillisec, delayAfterFirstEmptyPollMillisec)
        {
            this.asyncPollFunc = asyncPollFunc;
        }

        /// <summary>
        ///     Returns null if payload cannot be acquired, and non-null if payload is captured.
        /// </summary>
        /// <returns></returns>
        protected virtual TNonNullablePayload PollEasy()
        {
            if(this.asyncPollFunc == null)
                throw new InvalidDataException("Poll function must either be supplied to a constructor as a delegate, or Poll() method must be overridden in a subclass.");

            TNonNullablePayload payload = this.asyncPollFunc();
            return payload;
        }

        /// <summary>
        ///     Returns true if payload was acquired. False if poll call came back empty.
        ///     Can be overridden in subclasses.
        /// </summary>
        /// <returns></returns>
        protected override Tuple<bool, TNonNullablePayload> Poll()
        {
            TNonNullablePayload payload = this.PollEasy();
            return new Tuple<bool, TNonNullablePayload>(payload != null, payload);
        }

        /// <summary>
        ///     Blocks until either payload arrives, or polling is terminated.
        ///     Returns null if application is exiting or stop is signaled, or non-null if payload has arrived.
        /// </summary>
        public new TNonNullablePayload WaitForPayload()
        {
            Tuple<bool, TNonNullablePayload> result = base.WaitForPayload();
            return result.Item1 ? result.Item2 : null;
        }
    }
}