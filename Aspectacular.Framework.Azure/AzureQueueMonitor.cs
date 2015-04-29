#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Aspectacular
{
    /// <summary>
    ///     Implements callback control inversion for Azure regular (non-ESB) storage queue, enabling either
    ///     1) blocking wait for messages via WaitForPayload(), or
    ///     2) pub/sub pattern (callback on message arrival) via RegisterMessageHandler().
    /// </summary>
    /// <remarks>
    ///     This class deals with Azure Storage Queues' annoying lack of either blocking wait for messages,
    ///     or message arrival callback.
    ///     Background info: attempts of reading a message from Azure queues (non-EBS, regular storage queues) are
    ///     non-blocking, meaning that if there are no messages, queue message retriever returns immediately
    ///     with null response. This means that in order to read a message, a loop is required.
    ///     This loop, if done with no delays between attempts, will hog CPU and leak money as each queue check costs money.
    ///     This class implements smart queue check loop, with increasing delays - up to specified limit, ensuring that CPU
    ///     is free most of the time and saving money on
    /// </remarks>
    public class AzureQueuePicker : BlockingObjectPoll<IList<CloudQueueMessage>>
    {
        /// <summary>
        /// Maximum number of messages that can be picked from a queue at any given time.
        /// </summary>
        public const int MaxMessageCountForPicking = 32;

        protected readonly bool useAopProxyWhenAccessingQueue;
        protected QueueRequestOptions requestOptions;
        protected int messagePickCount = MaxMessageCountForPicking;
        
        /// <summary>
        /// Azure queue to be monitored (polled).
        /// </summary>
        public CloudQueue Queue { get; private set; }

        private readonly TimeSpan messageInvisibilityTime;

        /// <param name="queue">Azure queue to dequeue messages from.</param>
        /// <param name="messageInvisibilityTimeMillisec">
        ///     Time for queue element to be processed. If not deleted from queue within
        ///     this time, message is automatically placed back in the queue.
        /// </param>
        /// <param name="maxCheckDelaySeconds">Maximum delay, in seconds, between attempts to dequeue messages.</param>
        /// <param name="useAopProxyWhenAccessingQueue">
        ///     Set to true to use Aspectacular AOP proxy with process-wide set of aspects,
        ///     to call queue access functions. Set to false to call queue operations directly.
        /// </param>
        /// <param name="requestOptions"></param>
        /// <param name="messagePickCount">Number of messages to be dequeued at a time. Can't exceed MaxMessageCountForPicking.</param>
        public AzureQueuePicker(CloudQueue queue, int messageInvisibilityTimeMillisec, 
                        int maxCheckDelaySeconds = 60, 
                        bool useAopProxyWhenAccessingQueue = true,
                        QueueRequestOptions requestOptions = null,
                        int messagePickCount = MaxMessageCountForPicking
            )
            : base(null, maxCheckDelaySeconds * 1000)
        {
            this.Queue = queue;
            this.useAopProxyWhenAccessingQueue = useAopProxyWhenAccessingQueue;
            this.messageInvisibilityTime = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: messageInvisibilityTimeMillisec);
            this.requestOptions = requestOptions;
            this.messagePickCount = messagePickCount;
        }

        /// <summary>
        ///     Returns null if payload cannot be acquired, and non-null if payload is captured.
        /// </summary>
        /// <returns>Null if no messages were in the queue. List of dequeued messages otherwise.</returns>
        /// <remarks>Can be synchronized as it does "lock(this) {" before attempting to dequeue messages.</remarks>
        protected override IList<CloudQueueMessage> PollEasy()
        {
            IList<CloudQueueMessage> messages;

            OperationContext context = this.GetOperationContext();

            lock(this)
            {
                if(this.useAopProxyWhenAccessingQueue)
                    messages = this.Queue.GetProxy().List(q => q.GetMessages(messagePickCount, messageInvisibilityTime, this.requestOptions, context));
                else
                    messages = this.Queue.GetMessages(messagePickCount, messageInvisibilityTime, this.requestOptions, context).ToList();
            }

            return messages == null || messages.Count == 0 ? null : messages;
        }

        /// <summary>
        ///     Enables pub/sub patter by launching polling loop that invokes message-processing callback when messages arrived.
        ///     This is an alternative to using blocking WaitForPayload() method.
        /// </summary>
        /// <param name="messageProcessCallback">
        ///     Optional message-processing delegate. If null, Process() method must be overridden
        ///     in a subclass.
        /// </param>
        /// <remarks>
        ///     This method starts polling thread, on which both polling function and message processing functions are called.
        ///     This means that next attempt to dequeue messages won't occur until message processing callback function is done.
        ///     Payload processing callback may start its own thread(s) to process messages asynchronously and quickly return
        ///     control to the polling thread.
        /// </remarks>
        public void Subscribe(Action<CloudQueue, IList<CloudQueueMessage>> messageProcessCallback = null)
        {
            if(messageProcessCallback == null)
                // ReSharper disable once RedundantBaseQualifier
                base.Subscribe(payloadProcessCallback: null);
            else
                this.Subscribe(payload => messageProcessCallback(this.Queue, payload));
        }

        /// <summary>
        /// Returns OperationContext for picking a message from the queue.
        /// Default (base class) implementation returns null.
        /// </summary>
        /// <returns></returns>
        protected virtual OperationContext GetOperationContext()
        {
            return null;
        }
    }

    // ReSharper disable once CSharpWarnings::CS1591
    public static class AzureQueueExtensions
    {
        /// <summary>
        ///     Eliminates polling from getting a message off the regular Azure queue.
        ///     Blocks until either messages arrive, or smart polling is terminated.
        ///     Returns null if application is exiting or stop is signaled, otherwise non-empty collection of messages.
        ///     Uses smart polling with delays between attempts to dequeue messages, ensuring lows CPU utilization and not leaking
        ///     money for Azure storage transactions.
        /// </summary>
        /// <param name="queue">Azure queue to dequeue messages from.</param>
        /// <param name="messageInvisibilityTimeMillisec">
        ///     Time for queue element to be processed. If not deleted from queue within
        ///     this time, message is automatically placed back in the queue.
        /// </param>
        /// <param name="maxCheckDelaySeconds">Maximum delay, in seconds, between attempts to dequeue messages.</param>
        /// <param name="useAopProxyWhenAccessingQueue">
        ///     Set to true to use Aspectacular AOP proxy with process-wide set of aspects,
        ///     to call queue access functions. Set to false to call queue operations directly.
        /// </param>
        /// <returns>Returns null if application is exiting or stop is signaled, otherwise non-empty collection of messages.</returns>
        public static IList<CloudQueueMessage> WaitForMessages(this CloudQueue queue, int messageInvisibilityTimeMillisec, int maxCheckDelaySeconds = 60, bool useAopProxyWhenAccessingQueue = true)
        {
            if(queue == null)
                throw new ArgumentNullException("queue");

            using(var qmon = new AzureQueuePicker(queue, messageInvisibilityTimeMillisec, maxCheckDelaySeconds, useAopProxyWhenAccessingQueue))
            {
                IList<CloudQueueMessage> messages = qmon.WaitForPayload();
                return messages;
            }
        }

        /// <summary>
        ///     Enable pub/sub pattern for regular Azure queues instead of polling by
        ///     launching polling loop that invokes message-processing callback when messages arrived.
        ///     This is an alternative to using blocking WaitForMessage() method.
        ///     Uses smart polling with delays between attempts to dequeue messages, ensuring lows CPU utilization and not leaking
        ///     money for Azure storage transactions.
        /// </summary>
        /// <param name="queue">Azure queue to dequeue messages from.</param>
        /// <param name="messageProcessCallback">
        ///     Optional message-processing delegate. If null, Process() method must be overridden
        ///     in a subclass.
        /// </param>
        /// <param name="messageInvisibilityTimeMillisec">
        ///     Time for queue element to be processed. If not deleted from queue within
        ///     this time, message is automatically placed back in the queue.
        /// </param>
        /// <param name="maxCheckDelaySeconds">Maximum delay, in seconds, between attempts to dequeue messages.</param>
        /// <param name="useAopProxyWhenAccessingQueue">
        ///     Set to true to use Aspectacular AOP proxy with process-wide set of aspects,
        ///     to call queue access functions. Set to false to call queue operations directly.
        /// </param>
        /// <returns>
        ///     IDisposable queue wrapper object to be used later for calling Stop() or Dispose() methods to terminate queue
        ///     polling.
        /// </returns>
        /// <remarks>
        ///     This method starts polling thread, on which both polling function and message processing functions are called.
        ///     This means that next attempt to dequeue messages won't occur until message processing callback function is done.
        ///     Payload processing callback may start its own thread(s) to process messages asynchronously and quickly return
        ///     control to the polling thread.
        /// </remarks>
        public static AzureQueuePicker Subscribe(this CloudQueue queue, Action<CloudQueue, IList<CloudQueueMessage>> messageProcessCallback,
            int messageInvisibilityTimeMillisec, int maxCheckDelaySeconds = 60, bool useAopProxyWhenAccessingQueue = true)
        {
            if(queue == null)
                throw new ArgumentNullException("queue");
            if(messageProcessCallback == null)
                throw new ArgumentNullException("messageProcessCallback");

            var qmon = new AzureQueuePicker(queue, messageInvisibilityTimeMillisec, maxCheckDelaySeconds, useAopProxyWhenAccessingQueue);
            qmon.Subscribe(messageProcessCallback);
            return qmon;
        }
    }
}