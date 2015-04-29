using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Aspectacular
{
    /// <summary>
    /// Base class for defining connection to Azure queues
    /// </summary>
    public abstract class AzureQueueConnection
    {
        /// <summary>
        /// </summary>
        protected AzureQueueConnection()
        {
            this.lazyQueue = new Lazy<CloudQueue>(this.InitAzureQueue);
        }

        /// <summary>
        /// A name of the ConnectionString Role setting, that specifies 
        /// how to access a queue.
        /// </summary>
        public string ConnectionStringName { get; set; }

        /// <summary>
        /// Azure store connection string.
        /// Does not need to be set if ConnectionStringName is specified;
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// A name of the queue to be accessed.
        /// </summary>
        public string QueueName { get; set; }

        protected Lazy<CloudQueue> lazyQueue;
        
        /// <summary>
        /// Azure Queue opened with the settings defined by this class.
        /// </summary>
        protected internal CloudQueue Queue
        {
            get { return this.lazyQueue.Value; }
        }

        /// <summary>
        /// Delayed queue initializer.
        /// </summary>
        /// <returns></returns>
        protected virtual CloudQueue InitAzureQueue()
        {
            if (this.ConnectionStringName.IsBlank())
                return null;
            if (this.QueueName.IsBlank())
                return null;

            if (this.ConnectionString.IsBlank())
                this.ConnectionString = RoleEnvironment.GetConfigurationSettingValue(this.ConnectionStringName);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.ConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(this.QueueName);
            queue.CreateIfNotExists();
            return queue;
        }
    }


    /// <summary>
    /// Azure queue connection definition for a write-only queue.
    /// </summary>
    public class AzureDestinationQueueConnection : AzureQueueConnection
    {
        /// <summary>
        /// For serialization purposes only. Do not use directly. Use InitialInvisibilityDelay instead.
        /// </summary>
        [XmlAttribute]
        [DefaultValue(0)]
        public int InitialInvisibilityDelayMillisec
        {
            get { return this.InitialInvisibilityDelay == null ? 0 : (int)this.InitialInvisibilityDelay.Value.TotalMilliseconds; }
            set
            {
                if(value < 1)
                    this.InitialInvisibilityDelay = null;
                else
                    this.InitialInvisibilityDelay = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: value);
            }
        }

        /// <summary>
        /// Optional QueueRequestOptions for posting messages to the destination queue.
        /// </summary>
        [XmlIgnore]
        public QueueRequestOptions RequestOptions { get; set; }

        /// <summary>
        /// Provides a way to supply An Microsoft.WindowsAzure.Storage.OperationContext object 
        /// that represents the context for the current operation, when posting forwarding a message to the destination quueue.
        /// </summary>
        [XmlIgnore]
        public Func<AzureDestinationQueueConnection, OperationContext> ContextSupplier { get; set; }

        /// <summary>
        /// Specifies interval of time from now during which the message will be invisible.
        /// If null then the message will be visible immediately.
        /// </summary>
        [XmlIgnore]
        public TimeSpan? InitialInvisibilityDelay { get; set; }

        /// <summary>
        /// Posts message to the queue using context properties.
        /// </summary>
        /// <param name="outboundMessage"></param>
        /// <param name="ttl"></param>
        public virtual void AddMessage(CloudQueueMessage outboundMessage, TimeSpan? ttl = null)
        {
            OperationContext context = this.ContextSupplier == null ? null : this.ContextSupplier(this);
            this.Queue.AddMessage(outboundMessage, ttl, this.InitialInvisibilityDelay, this.RequestOptions, context);
        }
    }

    /// <summary>
    /// Azure queue connection definition for a read-write queue.
    /// </summary>
    public class AzureSourceQueueConnection : AzureQueueConnection
    {
        /// <summary>
        /// Defines for how long message becomes invisible after it has been dequeued. 
        /// If message is not deleted from the queue within this time, it will reappear in the queue and will be processed again.
        /// </summary>
        [XmlAttribute]
        public int MessageInivisibilityTimeMillisec { get; set; }

        /// <summary>
        /// For AzureQueueMonitor, defines maximum delay between polling attempts.
        /// </summary>
        [XmlAttribute]
        public int MaxDelayBetweenDequeueAttemptsSeconds { get; set; }
    }
}
