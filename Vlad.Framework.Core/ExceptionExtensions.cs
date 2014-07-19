#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Aspectacular
{
    [Serializable]
    [XmlRoot("Exception")]
    public class ExceptionDto
    {
        private readonly Exception exception;

        public ExceptionDto(Exception ex)
        {
            this.exception = ex;
        }

        public ExceptionDto()
        {
        }

        // ReSharper disable ValueParameterNotUsed
        public string Message
        {
            get { return this.exception.Message; }
            set { }
        }

        public string StackTrace
        {
            get { return this.exception.StackTrace; }
            set { }
        }

        public string Source
        {
            get { return this.exception.Source; }
            set { }
        }

        public string HelpLink
        {
            get { return this.exception.HelpLink; }
            set { }
        }

        public string TypeString
        {
            get { return this.exception.GetType().FormatCSharp(true); }
            set { }
        }

        // ReSharper restore ValueParameterNotUsed
    }

    public static class ExceptionExtensions
    {
        /// <summary>
        ///     Returns all exceptions in the chain starting from the outermost.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static IEnumerable<Exception> AllExceptionsBack(this Exception ex)
        {
            for(; ex != null; ex = ex.InnerException)
                yield return ex;
        }

        /// <summary>
        ///     Returns all exceptions in the chain starting from the innermost.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static IEnumerable<Exception> AllExceptions(this Exception ex)
        {
            return ex.AllExceptionsBack().Reverse();
        }

        // ReSharper disable once InconsistentNaming
        public const string defaultItemSeparator = "--------------------------------------------------------";

        /// <summary>
        ///     Returns string with the information of exception type and message.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string TypeAndMessage(this Exception ex)
        {
            return "{0}: {1}".SmartFormat(ex.GetType().FormatCSharp(), ex.Message);
        }

        /// <summary>
        ///     Returns string containing exception type, message and stack trace.
        ///     No inner exception information included.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string FullInfo(this Exception ex)
        {
            string retVal = "{0}\r\nSTACK:\r\n{1}".SmartFormat(ex.TypeAndMessage(), ex.StackTrace);
            return retVal;
        }

        /// <summary>
        ///     Joins text information from the chain of exceptions including inner ones.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="separator"></param>
        /// <param name="innerFirst">True to put innermost exception on top, false for outermost.</param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static string Consolidate(this Exception ex, string separator, bool innerFirst, Func<Exception, string> converter)
        {
            if(separator == null)
                separator = defaultItemSeparator;

            separator = "\r\n{0}\r\n".SmartFormat(separator);

            Func<Exception, IEnumerable<Exception>> iterator = innerFirst ? AllExceptions : new Func<Exception, IEnumerable<Exception>>(AllExceptionsBack);

            NonEmptyString retVal = string.Join(separator, iterator(ex).Select(converter));
            return retVal;
        }

        /// <summary>
        ///     Returns all messages in the exception chain (main + inner exceptions),
        ///     starting from the innermost one.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="separator"></param>
        /// <param name="innerFirst">True to put innermost exception on top, false for outermost.</param>
        /// <returns></returns>
        public static string ConsolidatedMessage(this Exception ex, string separator = null, bool innerFirst = true)
        {
            return Consolidate(ex, separator, innerFirst, e => ex.TypeAndMessage());
        }

        /// <summary>
        ///     Returns all stack traces in the exception chain (main + inner exceptions),
        ///     starting from the innermost one.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="separator"></param>
        /// <param name="innerFirst">True to put innermost exception on top, false for outermost.</param>
        /// <returns></returns>
        public static string ConsolidatedStack(this Exception ex, string separator = null, bool innerFirst = true)
        {
            return Consolidate(ex, separator, innerFirst, e => e.StackTrace);
        }

        /// <summary>
        ///     Returns exception information (messages + stack traces) in the exception chain (main + inner exceptions),
        ///     starting from the innermost one.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="separator"></param>
        /// <param name="innerFirst">True to put innermost exception on top, false for outermost.</param>
        /// <returns></returns>
        public static string ConsolidatedInfo(this Exception ex, string separator = null, bool innerFirst = true)
        {
            return Consolidate(ex, separator, innerFirst, e => e.FullInfo());
        }

        /// <summary>
        ///     Returns Exception-like object that can serialized to XML, JSON, etc.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static ExceptionDto ToSerializable(this Exception ex)
        {
            return ex == null ? null : new ExceptionDto(ex);
        }
    }
}