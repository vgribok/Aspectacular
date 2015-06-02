using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Aspectacular
{
    /// <summary>
    /// Represents a route between a single source queue and multiple destination queues.
    /// </summary>
    public class AzureQueueMulticastRoute : IDisposable, ICallLogger
    {
        protected AzureQueuePicker queueMonitor;

        // ReSharper disable once EmptyConstructor
        /// <summary>
        /// </summary>
        public AzureQueueMulticastRoute()
        {
            this.DestinationQueues = new List<AzureDestinationQueueConnection>();
        }

        #region Properties

        /// <summary>
        /// Specifies an Azure queue from which messages will be removed
        /// and posted to DestinationQueues.
        /// </summary>
        public AzureSourceQueueConnection SourceQueue { get; set; }

        /// <summary>
        /// Collection of destination Azure queue where messages removed from the SourceQueue
        /// will be cloned to.
        /// </summary>
        public List<AzureDestinationQueueConnection> DestinationQueues { get; set; }

        /// <summary>
        /// Message transformation hook. Set it to have an ability to modify the message after it picked 
        /// from the source queue and inserted into the destination queue.
        /// Ensure this is a thread-safe method as it may be called in parallel, and for out-of-order messages.
        /// </summary>
        [XmlIgnore]
        public Func<CloudQueueMessage, CloudQueueMessage> OptionalThreadSafeMessageTransformer { get; set; }

        /// <summary>
        /// Allows providing an alternative way of instantiating AzureQueuePicker or its subclass.
        /// </summary>
        [XmlIgnore]
        public Func<AzureQueueMulticastRoute,AzureQueuePicker> OptionalQueueMonitorInjector { get; set; }

        #endregion Properties

        /// <summary>
        /// Starts asynchronous Azure queue multicast relaying of messages for the route,
        /// and immediately returns control.
        /// </summary>
        /// <returns></returns>
        public bool BeginAsyncMessageForwarding()
        {
            this.EndMessageForwarding();

            if (this.SourceQueue.Queue == null)
            {
                this.LogWarning("Queue \"{0}\" failed to get initialized. Will not be polled.", this.SourceQueue.QueueName);
                return false;
            }

            if (this.DestinationQueues.IsNullOrEmpty())
            {
                this.LogInformation("Destination queues for source queue \"{0}\" are not specified. Source queue will not be polled.", this.SourceQueue.QueueName);
                return false;
            }

            int initializedDestinationQueueCount = this.DestinationQueues.Count(destinationQueue => destinationQueue.Queue != null);
            this.LogInformation("Initialized {0} of {1} destination queues for source queue \"{2}\".", initializedDestinationQueueCount, this.DestinationQueues.Count, this.SourceQueue.QueueName);

            lock (this)
            {
                this.queueMonitor = this.InstantiateQueueMonitor();
            }

            return true;
        }

        /// <summary>
        /// Stops route's message relay.
        /// </summary>
        public void EndMessageForwarding()
        {
            lock (this)
            {
                if (this.queueMonitor != null)
                {
                    queueMonitor.Stop();
                    this.queueMonitor = null;
                }
            }
        }

        #region Implementation Methods

        private AzureQueuePicker InstantiateQueueMonitor()
        {
            if (this.OptionalQueueMonitorInjector != null)
            {
                AzureQueuePicker qmon = this.OptionalQueueMonitorInjector(this);
                qmon.Subscribe(this.RelayMessages);
                return qmon;
            }

            return this.SourceQueue.Queue.Subscribe(
                this.RelayMessages,
                this.SourceQueue.MessageInivisibilityTimeMillisec,
                this.SourceQueue.MaxDelayBetweenDequeueAttemptsSeconds,
                useAopProxyWhenAccessingQueue: false
                );
        }

        /// <summary>
        /// Moves 
        /// </summary>
        /// <param name="sourceQueue"></param>
        /// <param name="inboundMessages"></param>
        protected void RelayMessages(CloudQueue sourceQueue, IList<CloudQueueMessage> inboundMessages)
        {
            this.GetProxy().Invoke(inst => inst.RelayMessagesInternal(sourceQueue, inboundMessages));
        }

        /// <summary>
        /// Receives messages from source queue and puts them into destination queues.
        /// </summary>
        /// <param name="sourceQueue"></param>
        /// <param name="inboundMessages"></param>
        private void RelayMessagesInternal(CloudQueue sourceQueue, IList<CloudQueueMessage> inboundMessages)
        {
            if (inboundMessages.IsNullOrEmpty())
                return;

            if (this.DestinationQueues.IsNullOrEmpty())
                throw new Exception("Cannot relay queue messages because destination queue(s) are not specified.");

            this.LogInformation("Received {0} messages from queue \"{1}\". Dispatching them to {2} destination queues.", inboundMessages.Count, sourceQueue.Name, this.DestinationQueues.Count);
            //messages.ForEach(msg => this.LogInformation("Received message at {0}:\r\n\"{1}\"\r\n", DateTimeOffset.Now, msg.AsString));

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            inboundMessages = this.TransformInboundMessages(inboundMessages, stopWatch);

            this.ForwardMessages(inboundMessages.Count, inboundMessages, stopWatch);

            this.DeleteSourceMessages(sourceQueue, inboundMessages, stopWatch);
        }

        private void ForwardMessages(int inboundMessageCount, IList<CloudQueueMessage> inboundMessages, Stopwatch stopWatch)
        {
            if(this.DestinationQueues.Count == 1)
                // Special case: no need to incur the overhead of parallelism.
                PostMessagesToDestQueue(this.DestinationQueues[0], inboundMessages);
            else
                // Push messages to destination queues in parallel.
                this.DestinationQueues.AsParallel().ForAll(destQueue => PostMessagesToDestQueue(destQueue, inboundMessages));

            this.LogInformation("Done forwarding {0} messages to {1} queues. Elapsed {2}.", inboundMessageCount, this.DestinationQueues.Count, stopWatch.Elapsed);
            stopWatch.Reset();
            stopWatch.Start();
        }

        private IList<CloudQueueMessage> TransformInboundMessages(IList<CloudQueueMessage> inboundMessages, Stopwatch stopWatch)
        {
            if(this.OptionalThreadSafeMessageTransformer == null)
                return inboundMessages;

            // Transform messages before requeueing them.

            if(inboundMessages.Count < 5)
                // When we have a few messages, transform them sequentially.
                inboundMessages = inboundMessages.Select(msg => this.OptionalThreadSafeMessageTransformer(msg)).ToList();
            else
                // When there are many messages, transform them in parallel.
                inboundMessages = inboundMessages.AsParallel().AsOrdered().Select(msg => this.OptionalThreadSafeMessageTransformer(msg)).ToList();

            this.LogInformation("Done transforming {0} messages. Elapsed {1}.", inboundMessages.Count, stopWatch.Elapsed);
            stopWatch.Reset();
            stopWatch.Start();

            return inboundMessages;
        }

        // ReSharper disable once UnusedParameter.Local
        private int PostMessagesToDestQueue(AzureDestinationQueueConnection destQueue, IEnumerable<CloudQueueMessage> inboundMessages)
        {
            var outboundMessages = this.ConvertInboundMessagesToOutbound(inboundMessages).ToList();

            outboundMessages.ForEach(outMsgTuple => this.PostMessageToDestQueue(destQueue, outMsgTuple.Item1, outMsgTuple.Item2));

            return outboundMessages.Count();
        }

        private IEnumerable<Tuple<CloudQueueMessage, TimeSpan?>> ConvertInboundMessagesToOutbound(IEnumerable<CloudQueueMessage> inboundMessages)
        {
            foreach(CloudQueueMessage inboundMessage in inboundMessages)
            {
                if(inboundMessage.DequeueCount > 1)
                    this.LogWarning("Message {0} from queue \"{1}\" has been dequeued before {2} times.", inboundMessage.Id, this.SourceQueue.Queue.Name, inboundMessage.DequeueCount);

                TimeSpan? ttl = inboundMessage.ExpirationTime == null ? (TimeSpan?)null : inboundMessage.ExpirationTime.Value - DateTimeOffset.Now;
                CloudQueueMessage outboundMessage = new CloudQueueMessage(inboundMessage.AsBytes);
                
                yield return new Tuple<CloudQueueMessage, TimeSpan?>(outboundMessage, ttl);
            }
        }

        private void PostMessageToDestQueue(AzureDestinationQueueConnection destQueue, CloudQueueMessage outboundMessage, TimeSpan? ttl)
        {
            CloudQueueMessage outMsg = outboundMessage;

            try
            {
                destQueue.AddMessage(outMsg, ttl);
            }
            catch(StorageException ex)
            {
                const int queueNotFound = -2146233088;
                if(ex.HResult != queueNotFound) // Queue not found
                    throw;

                this.LogWarning("Queue \"{0}\" not found. Recreating.", destQueue.Queue.Name);
                // Re-create the queue and retry
                destQueue.Queue.CreateIfNotExists();
                destQueue.AddMessage(outMsg, ttl);
            }
        }

        private void DeleteSourceMessages(CloudQueue sourceQueue, IList<CloudQueueMessage> inboundMessages, Stopwatch stopWatch)
        {
            inboundMessages.ForEach(inMsg => sourceQueue.DeleteMessage(inMsg));
            this.LogInformation("Done deleting {0} messages from {1} queue. Elapsed {2}.", inboundMessages.Count, sourceQueue.Name, stopWatch.Elapsed);
        }

        #endregion Implementation Methods

        #region Interface Implementations

        void IDisposable.Dispose()
        {
            this.EndMessageForwarding();
        }

        /// <summary>
        ///     An accessor to AOP logging functionality for intercepted methods.
        /// </summary>
        [XmlIgnore]
        IMethodLogProvider ICallLogger.AopLogger { get; set; }

        #endregion Interface Implementations
    }
}
