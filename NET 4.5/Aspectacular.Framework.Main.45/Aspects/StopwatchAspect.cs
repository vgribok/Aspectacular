using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Aspectacular
{
    /// <summary>
    /// An aspect recording start time and elapsed execution time.
    /// Place this aspect in the aspect collection AFTER log output classes, like TraceOutputAspect, to ensure final data point is shown in the log.
    /// </summary>
    public class StopwatchAspect : Aspect
    {
        /// <summary>
        /// When true, outputs time and elapsed time after every cut-point.
        /// Otherwise, output only start and end time and total elapsed time.
        /// </summary>
        public readonly bool Detailed = false;

        protected readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// </summary>
        /// <param name="detailed">If true, outputs time and elapsed time after every cut-point. Otherwise data recorded at the beginning and at the end.</param>
        public StopwatchAspect(bool detailed)
        {
            this.Detailed = detailed;
        }

        /// <summary>
        /// Initializes stopwatch that will record on start and end time and elapsed time. No interim measurements will be recorded.
        /// </summary>
        public StopwatchAspect()
        {
        }

        /// <summary>
        ///     Called for non-static methods only.
        ///     Please note that method metadata is not available at this point.
        /// </summary>
        public override void Step_1_BeforeResolvingInstance()
        {
            this.RecordStartTime();
        }

        private void RecordStartTime()
        {
            this.stopwatch.Start();
            this.LogInformationWithKey("Start time", "{0:O}", DateTimeOffset.Now);
        }

        /// <summary>
        ///     Called right before intercepted method execution.
        /// </summary>
        public override void Step_2_BeforeTryingMethodExec()
        {
            this.RecordStepTime("Step_2_BeforeTryingMethodExec");
        }

        /// <summary>
        ///     Called after intercepted method returned result and
        ///     before interceptor augmented it, usually by LINQ modifiers like List().
        ///     May be called multiple times if retries are enabled.
        /// </summary>
        /// <remarks>
        ///     LINQ's List(), Single(), etc. methods may be used to execute
        ///     query returned by the intercepted method. This interceptor
        ///     is called after query was returned and before it was executed.
        ///     During this period, Context.ReturnedValue has exact value
        ///     returned by the intercepted method.
        ///     After this interceptor is called, Context.ReturnedValue may be changed,
        ///     primarily by LINQ modifiers, like List().
        /// </remarks>
        public override void Step_3_BeforeMassagingReturnedResult()
        {
            this.RecordStepTime("Step_3_BeforeMassagingReturnedResult");
        }

        /// <summary>
        ///     Called after method execution failed (thrown an exception).
        /// </summary>
        public override void Step_4_Optional_AfterCatchingMethodExecException()
        {
            this.RecordStepTime("Step_4_Optional_AfterCatchingMethodExecException");
        }

        /// <summary>
        ///     Called after method itself and optional result massager were called successfully.
        ///     May be called multiple times when retries are enabled.
        /// </summary>
        /// <remarks>
        ///     Since this method may be called multiple times on retries,
        ///     put all finalization/cleanup logic into steps 5-7.
        /// </remarks>
        public override void Step_4_Optional_AfterSuccessfulCallCompletion()
        {
            this.RecordStepTime("Step_4_Optional_AfterSuccessfulCallCompletion");
        }

        /// <summary>
        ///     Called after method execution success or failure.
        /// </summary>
        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            this.RecordStepTime("Step_5_FinallyAfterMethodExecution");
        }

        /// <summary>
        ///     Called only for instance method that have instance cleanup
        /// </summary>
        public override void Step_6_Optional_AfterInstanceCleanup()
        {
            this.RecordStepTime("Step_6_Optional_AfterInstanceCleanup");
        }

        /// <summary>
        ///     The very final cutpoint in the life cycle of the call.
        /// </summary>
        public override void Step_7_AfterEverythingSaidAndDone()
        {
            this.RecordStepTime("Finished execution", forceDetail: true);
            this.stopwatch.Stop();
        }

        /// <summary>
        /// Records elapsed time for a given step.
        /// </summary>
        /// <param name="stepName"></param>
        /// <param name="forceDetail">Pass true to record data for the step even if this.Detail=false.</param>
        protected void RecordStepTime(string stepName, bool forceDetail = false)
        {
            if (!this.stopwatch.IsRunning)
            {
                this.RecordStartTime();
            }
            else if (this.Detailed || forceDetail)
            {
                this.LogInformationData("Elapsed time since start till " + stepName, this.stopwatch.Elapsed);
                this.LogInformationData("Elapsed ticks since start till " + stepName, stopwatch.ElapsedTicks);
            }
        }
    }
}
