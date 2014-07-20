using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aspectacular
{
    public class PollPseudoCallback<TPollRetVal> where TPollRetVal : class
    {
        private readonly ManualResetEvent stopSignal = new ManualResetEvent(initialState: false);
        private readonly Func<TPollRetVal> pollFunc;
        private readonly Func<TPollRetVal, bool> processFunc;

        public readonly int MaxPollSleepDelayMillisec;
        public readonly int DelayAfterFirstEmptyPollMillisec;

        public PollPseudoCallback(Func<TPollRetVal> pollFunc = null, 
                                  Func<TPollRetVal, bool> processFunc = null,  
                                  int maxPollSleepDelayMillisec = 60 * 1000, 
                                  int delayAfterFirstEmptyPollMillisec = 10,
                                  bool autoStart = true)
        {
            if(maxPollSleepDelayMillisec < 1)
                throw new ArgumentException("maxPollSleepDelayMillisec must be 1 or larger.");
            if(delayAfterFirstEmptyPollMillisec < 1)
                throw new ArgumentException("delayAfterFirstEmptyPollMillisec must be 1 or larger.");

            this.pollFunc = pollFunc;
            this.processFunc = processFunc;
            this.MaxPollSleepDelayMillisec = maxPollSleepDelayMillisec;
            this.DelayAfterFirstEmptyPollMillisec = delayAfterFirstEmptyPollMillisec;

            if(autoStart)
                this.StartSmartPolling();
        }

        protected virtual TPollRetVal Poll()
        {
            if(this.pollFunc == null)
                throw new InvalidDataException("Poll function must either be supplied to a constructor as a delegate, or Poll() method must be overridden in a subclass.");

            return this.pollFunc();
        }

        protected virtual bool Process(TPollRetVal polledValue)
        {
            if(this.processFunc == null)
                throw new InvalidDataException("Process function must either be supplied to a constructor as a delegate, or Process() method must be overridden in a subclass.");

            return this.processFunc(polledValue);
        }

        public void StartSmartPolling()
        {
            this.stopSignal.Reset();

            this.RunPollLoop();
        }

        private void RunPollLoop()
        {
            for (TPollRetVal polledValue = this.WaitTillGetValueOrToldToStop();
                polledValue != null; // If polledValue, it means loop was told to stop;
                polledValue = this.WaitTillGetValueOrToldToStop())
            {
                this.Process(polledValue);
            }
        }

        private TPollRetVal WaitTillGetValueOrToldToStop()
        {
            int delayMillisec = 0;
            int delayIncrementMillisec = this.DelayAfterFirstEmptyPollMillisec;

            var waiters = new WaitHandle[] { this.stopSignal, Threading.ApplicationExiting };

            while(WaitHandle.WaitAny(waiters, delayMillisec) < 0)
            {
                TPollRetVal polledValue = this.Poll();
                if(polledValue != null)
                    return polledValue;

                // Poll came back empty.

                if(delayMillisec >= this.MaxPollSleepDelayMillisec) 
                    continue; 
                
                // Increase delay
                delayMillisec += delayIncrementMillisec;

                if(delayMillisec > this.MaxPollSleepDelayMillisec)
                    // Ensure delay does not exceed specified maximum
                    delayMillisec = this.MaxPollSleepDelayMillisec;
                else
                    delayIncrementMillisec *= 2;
            }

            return null;
        }

        public void StopSmartPolling()
        {
            this.stopSignal.Set();
        }
    }
}
