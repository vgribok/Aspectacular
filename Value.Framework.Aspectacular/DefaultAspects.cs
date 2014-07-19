#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;

namespace Aspectacular
{
    /// <summary>
    ///     An element for DefaultAspectsConfigSection collection.
    /// </summary>
    public class DefaultAspect : ConfigurationElement
    {
        [ConfigurationProperty("type", IsKey = true, IsRequired = true)]
        public string TypeString
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        [ConfigurationProperty("constructorParameters", IsRequired = false)]
        public string ConstructorParameters
        {
            get { return (string)this["constructorParameters"]; }
            set { this["constructorParameters"] = value; }
        }

        /// <summary>
        ///     Aspect type
        /// </summary>
        public Type Type
        {
            get
            {
                Type type = Type.GetType(this.TypeString);
                if(type == null)
                    throw new Exception("Unable to find Type \"{0}\".".SmartFormat(this.TypeString));
                return type;
            }
        }

        /// <summary>
        ///     Parses semicolon-delimited parameter string,
        ///     which has same format as connection strings,
        ///     and extracts a value for given key.
        ///     Returns defaultValue if key was not found.
        /// </summary>
        /// <param name="delimiteParamString"></param>
        /// <param name="paramKey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetParameterValue(string delimiteParamString, string paramKey, string defaultValue = null)
        {
            DbConnectionStringBuilder paramStringParse = new DbConnectionStringBuilder
            {
                ConnectionString = delimiteParamString
            };

            object val;
            if(!paramStringParse.TryGetValue(paramKey, out val))
                return defaultValue;

            return val.ToStringEx();
        }
    }

    public class DefaultAspectCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new DefaultAspect();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
// ReSharper disable once PossibleNullReferenceException
            return (element as DefaultAspect).Type;
        }
    }

    /// <summary>
    ///     Custom configuration section provider
    ///     for default aspect collection.
    /// </summary>
    public class DefaultAspectsConfigSection : ConfigurationSection
    {
        //[ConfigurationProperty("appendToTail", DefaultValue = "false", IsRequired = false)]
        //public bool AppendToTail
        //{
        //    get
        //    {
        //        return (bool)this["appendToTail"];
        //    }
        //    set
        //    {
        //        this["appendToTail"] = false;
        //    }
        //}

        [ConfigurationProperty("aspects", IsRequired = false)]
        private DefaultAspectCollection DefaultAspects
        {
            get { return this["aspects"] as DefaultAspectCollection; }
        }

        private static readonly Lazy<List<Func<Aspect>>> configAspectActivators;

        static DefaultAspectsConfigSection()
        {
            configAspectActivators = new Lazy<List<Func<Aspect>>>(LoadDefaultAspectConfig);
        }

        public static DefaultAspectsConfigSection LoadConfigSection()
        {
            try
            {
                DefaultAspectsConfigSection config = (DefaultAspectsConfigSection)ConfigurationManager.GetSection("defaultAspects");
                return config;
            }
            catch(ConfigurationException)
            {
                // <defaultAspects> section missing in the .config file leads to this exception.
                return null;
            }
        }

        /// <summary>
        ///     Load default aspects from the .config custom section.
        /// </summary>
        /// <returns>Collection of fast aspect rawActivator delegates.</returns>
        public static List<Func<Aspect>> LoadDefaultAspectConfig()
        {
            DefaultAspectsConfigSection config = LoadConfigSection();

            var initializators = new List<Func<Aspect>>();

            if(config != null && !config.DefaultAspects.IsNullOrEmpty())
            {
                foreach(DefaultAspect defaultAspectInfo in config.DefaultAspects)
                {
                    Type aspectType = defaultAspectInfo.Type;
                    string constructorParams = defaultAspectInfo.ConstructorParameters;

                    List<string> parms = new List<string>();

                    if(!constructorParams.IsBlank())
                        parms.Add(constructorParams);

                    try
                    {
                        object[] constructorArgs = parms.Cast<object>().ToArray();

                        Func<object> rawActivator = aspectType.GetFastActivatorWithEmbeddedArgs(constructorArgs);
                        Func<Aspect> activator = () => (Aspect)rawActivator();
                        initializators.Add(activator);
                    }
                    catch(Exception ex)
                    {
                        throw new Exception("Failed to create activators for the default set of aspects from .config file. See inner exception information.", ex);
                    }
                }
            }

            return initializators;
        }

        internal static IEnumerable<Aspect> GetConfigAspects()
        {
            try
            {
                return configAspectActivators.Value.Select(activator => activator());
            }
            catch(Exception ex)
            {
                throw new Exception("Failed to instantiate a default aspect. See inner exception information.".SmartFormat(), ex);
            }
        }
    }
}