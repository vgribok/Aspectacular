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
        protected AzureQueueMonitor queueMonitor;

        // ReSharper disable once EmptyConstructor
        /// <summary>
        /// </summary>
        public AzureQueueMulticastRoute()
        {
            this.DestinationQueues = new List<AzureDestinationQueueConnection>();
        }

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
        /// </summary>
        [XmlIgnore]
        public Func<CloudQueueMessage, CloudQueueMessage> OptionalThreadSafeMessageTransformer { get; set; }

        /// <summary>
        /// Starts Azure queue multicast relay of messages for the route.
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            this.Stop();

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
                this.queueMonitor = this.SourceQueue.Queue.Subscribe(
                    this.RelayMessages,
                    this.SourceQueue.MessageInivisibilityTimeMillisec,
                    this.SourceQueue.MaxDelayBetweenDequeueAttemptsSeconds,
                    useAopProxyWhenAccessingQueue: false
                    );
            }

            return true;
        }

        /// <summary>
        /// Moves 
        /// </summary>
        /// <param name="sourceQueue"></param>
        /// <param name="inboundMessages"></param>
        protected void RelayMessages(CloudQueue sourceQueue, IList<CloudQueueMessage> inboundMessages)
        {
            this.GetProxy().Invoke(() => RelayMessagesInternal(sourceQueue, inboundMessages));
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

            if(this.OptionalThreadSafeMessageTransformer != null)
            {   // Transform messages before requeueing them.
                
                if(inboundMessages.Count < 5)
                    // When we have a few messages, transform them sequentially.
                    inboundMessages = inboundMessages.Select(msg => this.OptionalThreadSafeMessageTransformer(msg)).ToList();
                else
                    // When there are many messages, transform them in parallel.
                    inboundMessages = inboundMessages.AsParallel().AsOrdered().Select(msg => this.OptionalThreadSafeMessageTransformer(msg)).ToList();

                this.LogInformation("Done transforming {0} messages. Elapsed {1}.", inboundMessages.Count, stopWatch.Elapsed);
                stopWatch.Reset();
                stopWatch.Start();
            }

            IEnumerable<byte[]> messageBodies = inboundMessages.Select(msg => msg.AsBytes);

            if (this.DestinationQueues.Count == 1)
                // Special case: no need to incur the overhead of parallelism.
                PostMessagesToDestQueue(this.DestinationQueues[0].Queue, messageBodies);
            else
                // Push messages to destination queues in parallel.
                this.DestinationQueues.AsParallel().ForAll(destQueue => PostMessagesToDestQueue(destQueue.Queue, messageBodies));

            this.LogInformation("Done posting {0} messages in {1} queues. Elapsed {2}.", inboundMessages.Count, this.DestinationQueues.Count, stopWatch.Elapsed);
            stopWatch.Reset();
            stopWatch.Start();

            inboundMessages.ForEach(inMsg => sourceQueue.DeleteMessage(inMsg));
            this.LogInformation("Done deleting {0} messages from {1} queue. Elapsed {2}.", inboundMessages.Count, sourceQueue.Name, stopWatch.Elapsed);
        }

        // ReSharper disable once UnusedParameter.Local
        private int PostMessagesToDestQueue(CloudQueue destQueue, IEnumerable<byte[]> messageBodies)
        {
            IEnumerable<CloudQueueMessage> outboudMessages = messageBodies.Select(msgBody => new CloudQueueMessage(msgBody));

            int count = 0;
            foreach (CloudQueueMessage outboundMessage in outboudMessages)
            {
                TimeSpan? ttl = outboundMessage.ExpirationTime == null ? (TimeSpan?)null : outboundMessage.ExpirationTime.Value - DateTimeOffset.Now;

                CloudQueueMessage outMsg = outboundMessage;

                try
                {
                    destQueue.AddMessage(outMsg, ttl, initialVisibilityDelay: null, options: null, operationContext: null);
                }
                catch (StorageException ex)
                {
                    const int queueNotFound = -2146233088;
                    if (ex.HResult != queueNotFound) // Queue not found
                        throw;

                    this.LogWarning("Queue \"{0}\" not found. Recreating.", destQueue.Name);
                    // Re-create the queue and retry
                    destQueue.CreateIfNotExists();
                    destQueue.AddMessage(outMsg, ttl, initialVisibilityDelay: null, options: null, operationContext: null);
                }
                count++;
            }

            return count;
        }

        public void Dispose()
        {
            this.Stop();
        }

        /// <summary>
        /// Stops route's message relay.
        /// </summary>
        public void Stop()
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

        /// <summary>
        ///     An accessor to AOP logging functionality for intercepted methods.
        /// </summary>
        public IMethodLogProvider AopLogger { get; set; }
    }

    /// <summary>
    /// Represents XML-serializable collection of queue dispatcher multicast routes.
    /// </summary>
    [XmlRoot(ElementName = "AzureQueueMulticastRoutes")]
    public class AzureQueueMulticastRouteConfiguration : List<AzureQueueMulticastRoute>
    {
        /*
         *  Example of the route configuration stored as XML:
		 
            <AzureQueueMulticastRoutes xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                <AzureQueueMulticastRoute>
                    <SourceQueue MessageInivisibilityTimeMillisec="10000" MaxDelayBetweenDequeueAttemptsSeconds="15">
                        <ConnectionStringName>QueueStorageAccountConnectionString</ConnectionStringName>
                        <QueueName>scheduletriggers</QueueName>
                    </SourceQueue>
                    <DestinationQueues>
                        <AzureDestinationQueueConnection>
                            <ConnectionStringName>QueueStorageAccountConnectionString</ConnectionStringName>
                            <QueueName>destionationuno</QueueName>
                        </AzureDestinationQueueConnection>
                        <AzureDestinationQueueConnection>
                            <ConnectionStringName>QueueStorageAccountConnectionString</ConnectionStringName>
                            <QueueName>destionationdos</QueueName>
                        </AzureDestinationQueueConnection>
                    </DestinationQueues>
                </AzureQueueMulticastRoute>
            </AzureQueueMulticastRoutes>         
         */

        /// <summary>
        /// Loads Azure queue multicast route configuration from XML.
        /// </summary>
        /// <param name="formatForRoleSettings">If true, CR/LFs get removed from XML so XML could be stored as a value of an Azure role setting.</param>
        /// <param name="defaultNamespace">XML default namespace.</param>
        /// <param name="settings">XML serialization settings.</param>
        /// <returns></returns>
        public string ToXml(bool formatForRoleSettings, string defaultNamespace = null, XmlWriterSettings settings = null)
        {
            if (formatForRoleSettings)
            {
                if (settings == null)
                    settings = new XmlWriterSettings();

                settings.NewLineHandling = NewLineHandling.Replace;
                settings.NewLineChars = " ";

                settings.Indent = false;
                settings.Encoding = Encoding.UTF8;
                settings.OmitXmlDeclaration = true;
            }

            // ReSharper disable once InvokeAsExtensionMethod
            string xml = this.ToXml(defaultNamespace, settings);

            return xml;
        }

        /// <summary>
        /// Loads Azure queue multicasting relay route configuration from XML stored as a Role setting value.
        /// </summary>
        /// <param name="azureRoleSettingName"></param>
        /// <returns></returns>
        public static AzureQueueMulticastRouteConfiguration LoadFromAzureRoleSettings(string azureRoleSettingName = "AzureQueueMulticastRoutes")
        {
            if (azureRoleSettingName.IsBlank())
                azureRoleSettingName = "AzureQueueMulticastRoutes";

            string routeConfigXml = RoleEnvironment.GetConfigurationSettingValue(azureRoleSettingName);
            return LoadFromXml(routeConfigXml);
        }

        /// <summary>
        /// Loads message routing information from XML.
        /// </summary>
        /// <param name="routeConfigXml">Example:
        /// <![CDATA[
        ///             <AzureQueueMulticastRoutes xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
        ///         <AzureQueueMulticastRoute>
        ///             <SourceQueue MessageInivisibilityTimeMillisec="10000" MaxDelayBetweenDequeueAttemptsSeconds="15">
        ///                 <ConnectionStringName>QueueStorageAccountConnectionString</ConnectionStringName>
        ///                 <QueueName>scheduletriggers</QueueName>
        ///            </SourceQueue>
        ///            <DestinationQueues>
        ///                <AzureDestinationQueueConnection>
        ///                    <ConnectionStringName>QueueStorageAccountConnectionString</ConnectionStringName>
        ///                    <QueueName>destionationone</QueueName>
        ///                </AzureDestinationQueueConnection>
        ///                <AzureDestinationQueueConnection>
        ///                    <ConnectionStringName>QueueStorageAccountConnectionString</ConnectionStringName>
        ///                    <QueueName>destionationtwo</QueueName>
        ///                </AzureDestinationQueueConnection>
        ///            </DestinationQueues>
        ///        </AzureQueueMulticastRoute>
        ///    </AzureQueueMulticastRoutes>         
        /// ]]>
        /// </param>
        /// <returns></returns>
        /// 
        public static AzureQueueMulticastRouteConfiguration LoadFromXml(string routeConfigXml)
        {
            AzureQueueMulticastRouteConfiguration routeConfig = routeConfigXml.FromXml<AzureQueueMulticastRouteConfiguration>();
            return routeConfig;
        }

        /// <summary>
        /// Starts multicast relay of Azure queue messages for all routes.
        /// </summary>
        /// <returns>Count of routes that have started successfully.</returns>
        public int Start()
        {
            int successfulStartCount = this.Count(route => route.Start());

            if (successfulStartCount != this.Count)
                Trace.TraceInformation("Started {0} of {1} azure queue multicasting routes.", successfulStartCount, this.Count);

            return successfulStartCount;
        }

        /// <summary>
        /// Stops relaying Azure queue messages for all routes.
        /// </summary>
        public void Stop()
        {
            this.ForEach(route => route.Stop());
        }
    }
}
