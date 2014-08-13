using System;
using System.Diagnostics;

namespace Aspectacular.Aspects
{
    public class TimerAspect : Aspect
    {
        private readonly Stopwatch _frameworkStopwatch;
        private readonly Stopwatch _methodStopwatch;

        public TimerAspect()
        {
            _frameworkStopwatch = new Stopwatch();
            _methodStopwatch = new Stopwatch();
        }

        public override void Step_1_BeforeResolvingInstance()
        {
            _frameworkStopwatch.Start();
        }

        public override void Step_2_BeforeTryingMethodExec()
        {
            _methodStopwatch.Start();
        }

        public override void Step_3_BeforeMassagingReturnedResult()
        {
            
        }

        public override void Step_4_Optional_AfterSuccessfulCallCompletion()
        {
            
        }

        public override void Step_4_Optional_AfterCatchingMethodExecException()
        {
            
        }

        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            _methodStopwatch.Stop();
        }

        public override void Step_6_Optional_AfterInstanceCleanup()
        {
            
        }

        public override void Step_7_AfterEverythingSaidAndDone()
        {
            _frameworkStopwatch.Stop();

            LogElapsedTime("Framework Overhead", _frameworkStopwatch.Elapsed.Subtract(_methodStopwatch.Elapsed));
            LogElapsedTime("Method Execution Time", _methodStopwatch.Elapsed);
        }

        private void LogElapsedTime(string label, TimeSpan ts)
        {
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds/10);

            LogInformationData(label, elapsedTime);
        }
    }
}