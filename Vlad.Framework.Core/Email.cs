using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Aspectacular
{
    public static class EmailHelper
    {
        public static void SendSmtpEmail(bool isBodyHtml, NonEmptyString optioanlFromAddress, NonEmptyString optionalReplyToAddress, string subject, string body, params string[] toAddresses)
        {
            if (toAddresses != null)
                toAddresses = toAddresses.Where(addr => !addr.IsBlank()).ToArray();

            if (toAddresses.IsNullOrEmpty())
                throw new Exception("\"To\" address must be specified");

            if (subject.IsBlank() && body.IsBlank())
                throw new Exception("Both subject and message body cannot be blank.");

            MailMessage message = new MailMessage();

            if (optioanlFromAddress != null)
                message.From = new MailAddress(optioanlFromAddress);

            if (optionalReplyToAddress != null)
                message.ReplyToList.Add(new MailAddress(optionalReplyToAddress));

            toAddresses.ForEach(toAddr => message.To.Add(new MailAddress(toAddr)));

            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isBodyHtml;

            SmtpClient smtpClient = new SmtpClient { Timeout = 10 * 1000 };
            smtpClient.Send(message);
        }

        /// <summary>
        /// Global email address format check regular expression pattern.
        /// </summary>
        public static readonly string emailCheckRegexPattern = @"(?<UserBeforeAt>[\w\.]{2,}) @ (?<Domain> (?<DomainMain>.{2,})  \. (?<DomainSuffix> \w{2,6}) )".Replace(" ", string.Empty);

        /// <summary>
        /// Global email address format check regular expression.
        /// </summary>
        public static Regex emailFormatRegex = new Regex(emailCheckRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Parses string using regular expression pattern, and returns following named tokens:
        /// UserBeforeAt, Domain, DomainMain, DomainSuffix
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public static Match ParseEmailAddress(this string emailAddress)
        {
            if (emailAddress == null)
                return null;

            return emailFormatRegex.Match(emailAddress);
        }

        /// <summary>
        /// Returns true if text matches email address regular expression pattern.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public static bool IsValidEmailFormat(this string emailAddress)
        {
            if (emailAddress == null)
                return false;

            return emailFormatRegex.IsMatch(emailAddress);
        }
    }
}
