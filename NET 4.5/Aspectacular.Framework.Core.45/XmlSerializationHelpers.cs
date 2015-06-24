using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Aspectacular
{
    public static class XmlSerializationHelpers
    {
        /// <summary>
        /// Deserializes xml text into an object of specified type.
        /// </summary>
        /// <typeparam name="T">Type of the deserialized object.</typeparam>
        /// <param name="xml">XML text to be parsed and deserialized.</param>
        /// <param name="defaultNamespace">Optional, default XML namespace</param>
        /// <returns>Deserialized object</returns>
        public static T FromXml<T>(this string xml, string defaultNamespace = null)
            where T : class, new()
        {
            var xmlSer = new XmlSerializer(typeof(T), defaultNamespace);

            using (StringReader reader = new StringReader(xml))
            {
                T obj = (T)xmlSer.Deserialize(reader);
                return obj;
            }
        }

        /// <summary>
        ///     Serializes object to XML
        /// </summary>
        /// <param name="obj">A POCO, or </param>
        /// <param name="defaultNamespace"></param>
        /// <param name="settings"></param>
        /// <returns>XML text of the serialized object.</returns>
        public static string ToXml(this object obj, string defaultNamespace = null, XmlWriterSettings settings = null)
        {
            if (obj == null)
                return null;

            if (obj is string)
                throw new ArgumentException("Cannot convert string to XML.");

            //if(!obj.GetType().IsSerializable)
            //    throw new ArgumentException("Object must be serializable in order to be converted to XML.");

            if (settings == null)
                settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = true,
                };

            StringBuilder sb = new StringBuilder();

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                XmlDocument xmlDoc = obj as XmlDocument;
                if (xmlDoc != null)
                    xmlDoc.Save(writer);
                else
                {
                    XmlSerializer ser = new XmlSerializer(obj.GetType(), defaultNamespace);
                    ser.Serialize(writer, obj);
                }
            }

            string xml = sb.ToString();
            return xml;
        }

        /// <summary>
        /// Serializes object as a new XmlNode into the content of an existing parentXmlNode
        /// </summary>
        /// <param name="obj">Object to be XML-serialized</param>
        /// <param name="parentXmlNode">XmlNode to serialize into</param>
        /// <param name="trueAppendFalseInsertFirst">If true, new XmlElement is appended at the end of the outerXmlElement content. Otherwise it's added at the top of the XmlElement content.</param>
        /// <param name="defaultNamespace"></param>
        /// <returns>Newly-create XmlNode</returns>
        public static XmlNode SerializeInto(this object obj, XmlNode parentXmlNode, bool trueAppendFalseInsertFirst = true, string defaultNamespace = null)
        {
            if (obj == null)
                return null;

            if (obj is string)
                throw new ArgumentException("Cannot convert string to XML.");

            if (parentXmlNode == null)
                throw new ArgumentNullException("parentXmlNode");

            XmlDocument xmlDoc = obj.ToXmlDocument(defaultNamespace);
            // ReSharper disable once PossibleNullReferenceException
            XmlNode newNode = parentXmlNode.OwnerDocument.ImportNode(xmlDoc.FirstChild, deep: true);

            if(trueAppendFalseInsertFirst)
                parentXmlNode.AppendChild(newNode);
            else 
                parentXmlNode.PrependChild(newNode);

            return newNode;
        }

        /// <summary>
        /// Serializes object as a new XmlDocument
        /// </summary>
        /// <param name="obj">Object to be XML-serialized</param>
        /// <param name="defaultNamespace"></param>
        /// <returns></returns>
        public static XmlDocument ToXmlDocument(this object obj, string defaultNamespace = null)
        {
            if (obj == null)
                return null;

            if (obj is string)
                throw new ArgumentException("Cannot convert string to XML.");

            XmlDocument xmlDoc = new XmlDocument();
            XPathNavigator nav = xmlDoc.CreateNavigator();

            using (XmlWriter writer = nav.AppendChild())
            {
                XmlSerializer ser = new XmlSerializer(obj.GetType(), defaultNamespace);
                ser.Serialize(writer, obj);
            }

            return xmlDoc;
        }

        /// <summary>
        /// Efficiently deserializes an XmlNode into a strongly-typed object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlNode"></param>
        /// <param name="defaultNamespace">Optional, default XML namespace</param>
        /// <returns></returns>
        public static T Deserialize<T>(this XmlNode xmlNode, string defaultNamespace = null) where T : new()
        {
            using (XmlReader reader = new XmlNodeReader(xmlNode))
            {
                XmlSerializer ser = new XmlSerializer(typeof(T), defaultNamespace);
                T obj = (T)ser.Deserialize(reader);
                return obj;
            }
        }

        public static T Deserialize<T>(this XmlDocument xmlDocument) where T : new()
        {
            return xmlDocument.DocumentElement.Deserialize<T>();
        }
    }
}
