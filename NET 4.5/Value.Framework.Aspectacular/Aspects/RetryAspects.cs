#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;

namespace Aspectacular
{
    /// <summary>
    ///     Defines signature for a custom method deciding whether
    ///     intercepted method's returned value or exception require retry.
    /// </summary>
    /// <param name="aopProxy">AOP Proxy providing access to intercepted method's returned value or invocation exception.</param>
    /// <returns>True to retry, false not to retry.</returns>
    public delegate bool FailureDetectorDelegate(Proxy aopProxy);

    /// <summary>
    ///     Enables method retrying
    /// </summary>
    public abstract class RetryAspectBase : Aspect
    {
        /// <summary>
        ///     Specifies delay between retry attempts.
        /// </summary>
        public uint MillisecDelayBetweenRetries { get; set; }

        /// <summary>
        ///     Optional callback allowing custom logic to analyze returned method value
        ///     to decide whether call needs to be retried.
        ///     If not specified, only failed calls (when exception is thrown) would be retried.
        ///     Either return value or an Exception is passed
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private readonly FailureDetectorDelegate OptionalFailureDetector;

        /// <summary>
        /// </summary>
        /// <param name="millisecDelayBetweenRetries">Optional delay in milliseconds between retries.</param>
        /// <param name="optionalFailureDetector">Optional custom method to decide whether failure occurred and retry is required. If not provided, exception in the main method or result post-processing will trigger retry.</param>
        protected RetryAspectBase(uint millisecDelayBetweenRetries, FailureDetectorDelegate optionalFailureDetector)
        {
            this.MillisecDelayBetweenRetries = millisecDelayBetweenRetries;
            this.OptionalFailureDetector = optionalFailureDetector;
        }

        /// <summary>
        ///     Should be overwritten to implement actual logic deciding whether it needs to keep retrying.
        /// </summary>
        /// <returns>True to abort retry loop, false to continue retrying.</returns>
        protected abstract bool NeedToStopRetries();

        protected abstract void BeforeFirstRetry();

        public override void Step_4_Optional_AfterSuccessfulCallCompletion()
        {
            bool mayNeedToRetry = this.OptionalFailureDetector != null && this.OptionalFailureDetector(this.Proxy);
            this.SetRetryIfNecessary(mayNeedToRetry);
        }

        public override void Step_4_Optional_AfterCatchingMethodExecException()
        {
            bool mayNeedToRetry = this.OptionalFailureDetector == null || this.OptionalFailureDetector(this.Proxy);
            this.SetRetryIfNecessary(mayNeedToRetry);
        }

        protected virtual void SetRetryIfNecessary(bool mayNeedToRetry)
        {
            if(!mayNeedToRetry || this.NeedToStopRetries())
                return;

            this.Proxy.ShouldRetryCall = true;

            if(this.Proxy.AttemptsMade == 1)
            {
                this.LogInformationData("Retrying", true);
                this.BeforeFirstRetry();
            }

            object logData = this.Proxy.MethodExecutionException ?? this.Proxy.ReturnedValue;
            this.LogInformationData("Retry Cause", logData);

            if(this.MillisecDelayBetweenRetries > 0)
            {
                this.LogInformationWithKey("Retry delay", "{0:#,#0}", this.MillisecDelayBetweenRetries);

                if(Threading.Sleep(this.MillisecDelayBetweenRetries) == SleepResult.Aborted)
                {
                    // Application exiting. Abort retry loop.
                    this.Proxy.ShouldRetryCall = false;
                    this.LogInformationData("Retry Aborted", true);
                    return;
                }
            }

            this.LogInformationData("Retry Attempt", this.Proxy.AttemptsMade);
        }
    }

    /// <summary>
    ///     Aspect implementing a certain number of function call retries
    ///     either when method has failed or when certain result was returned.
    /// </summary>
    public class RetryCountAspect : RetryAspectBase
    {
        /// <summary>
        ///     Maximum number of attempts to call the function.
        /// </summary>
        public byte RetryCount { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="retryCount">Maximum number of attempts to call the function</param>
        /// <param name="millisecDelayBetweenRetries"></param>
        /// <param name="optionalFailureDetector">Optional custom method to decide whether failure occurred and retry is required. If not provided, exception in the main method or result post-processing will trigger retry.</param>
        public RetryCountAspect(byte retryCount, uint millisecDelayBetweenRetries = 0, FailureDetectorDelegate optionalFailureDetector = null)
            : base(millisecDelayBetweenRetries, optionalFailureDetector)
        {
            this.RetryCount = retryCount;
        }

        protected override bool NeedToStopRetries()
        {
            return this.Proxy.AttemptsMade >= this.RetryCount;
        }

        protected override void BeforeFirstRetry()
        {
            this.LogInformationData("Retries to be attempted", this.RetryCount);
        }
    }

    /// <summary>
    ///     Aspect retrying function call for a given time period
    ///     either when method has failed or when certain result was returned.
    /// </summary>
    public class RetryTimeAspect : RetryAspectBase
    {
        public DateTime FirstAttemptStart { get; protected set; }

        /// <summary>
        ///     Amount of time in milliseconds during which
        ///     attempts will be made to call the intercepted function.
        /// </summary>
        public uint KeepTryingForMilliseconds { get; private set; }

        /// <summary>
        /// </summary>
        /// <param name="keepTryingForMilliseconds">Time period during which attempts will be made to call intercepted method.</param>
        /// <param name="millisecDelayBetweenRetries"></param>
        /// <param name="optionalFailureDetector">Optional custom method to decide whether failure occurred and retry is required. If not provided, exception in the main method or result post-processing will trigger retry.</param>
        public RetryTimeAspect(uint keepTryingForMilliseconds, uint millisecDelayBetweenRetries = 0, FailureDetectorDelegate optionalFailureDetector = null)
            : base(millisecDelayBetweenRetries, optionalFailureDetector)
        {
            this.KeepTryingForMilliseconds = keepTryingForMilliseconds;
        }

        public override void Step_2_BeforeTryingMethodExec()
        {
            this.FirstAttemptStart = DateTime.UtcNow;
        }

        protected override bool NeedToStopRetries()
        {
            uint millisecElapsedSinceBeforeFirstAttempt = (uint)(DateTime.UtcNow - this.FirstAttemptStart).TotalMilliseconds;
            return millisecElapsedSinceBeforeFirstAttempt > this.KeepTryingForMilliseconds;
        }

        protected override void BeforeFirstRetry()
        {
            this.LogInformationWithKey("Retry time period (milliseconds)", "{0:#,#0}", this.KeepTryingForMilliseconds);
        }
    }

    /// <summary>
    /// Aspect that will keep retrying method call for a certain amount of time, 
    /// with ever increasing delays between retries.
    /// </summary>
    public class RetryExponentialDelayAspect : RetryTimeAspect
    {
        public double DelayMultiplier { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keepTryingForMilliseconds">Maximum amount of time to keep retrying for, after which failure is considered permanent.</param>
        /// <param name="initialMillisecDelayBetweenRetries">Delay after first failed attempt. Delay between subsequent attempts will grow.</param>
        /// <param name="delayMultiplier">Multiplier determining growth of delay between subsequent attempts.</param>
        /// <param name="optionalFailureDetector">Optional custom method to decide whether failure occurred and retry is required. If not provided, exception in the main method or result post-processing will trigger retry.</param>
        public RetryExponentialDelayAspect(uint keepTryingForMilliseconds, uint initialMillisecDelayBetweenRetries = 1, double delayMultiplier = 2.0, FailureDetectorDelegate optionalFailureDetector = null)
            : base(keepTryingForMilliseconds, initialMillisecDelayBetweenRetries == 0 ? 1 : initialMillisecDelayBetweenRetries, optionalFailureDetector)
        {
            if(delayMultiplier <= 1)
                throw new ArgumentException("delayMultiplier has to be greater than 1. {0} is an invalid value.".SmartFormat(delayMultiplier));

            this.DelayMultiplier = delayMultiplier;
        }

        protected override void SetRetryIfNecessary(bool mayNeedToRetry)
        {
            if (mayNeedToRetry && this.Proxy.AttemptsMade > 1)
                this.MillisecDelayBetweenRetries = (uint)(this.MillisecDelayBetweenRetries * this.DelayMultiplier); 

            base.SetRetryIfNecessary(mayNeedToRetry);
        }
    }
}