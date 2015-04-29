using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Aspectacular
{
    /// <summary>
    /// Represents XML-serializable collection of queue dispatcher multicast routes.
    /// </summary>
    [XmlRoot(ElementName = "AzureQueueMulticastRoutes")]
    public class AzureQueueMulticastRouteConfiguration : List<AzureQueueMulticastRoute>
    {
        /// <summary>
        /// Starts asynchronous multicast relay of Azure queue messages for all routes,
        /// and immediately returns control.
        /// </summary>
        /// <returns>Count of routes that have started successfully.</returns>
        public int BeginAsyncMessageForwarding()
        {
            int successfulStartCount = this.Count(route => route.BeginAsyncMessageForwarding());

            if (successfulStartCount != this.Count)
                Trace.TraceInformation("Started {0} of {1} azure queue multicasting routes.", successfulStartCount, this.Count);

            return successfulStartCount;
        }

        /// <summary>
        /// Stops relaying Azure queue messages for all routes.
        /// </summary>
        public void EndMessageForwarding()
        {
            this.ForEach(route => route.EndMessageForwarding());
        }

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
    }
}
