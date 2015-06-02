#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Aspectacular
{
    public enum EmailAddressParts
    {
        /// <summary>
        ///     In john.doe+junk@domain.com, it's "john.doe+junk"
        /// </summary>
        UserBeforeAt,

        /// <summary>
        ///     In john.doe+spam@domain.com, it's "john.doe"
        /// </summary>
        UserBeforePlus,

        /// <summary>
        ///     In john.doe+spam@domain.com, it's "spam"
        /// </summary>
        UserAfterPlusFilter,

        /// <summary>
        ///     In john.doe+spam@first.domain.com, it's "first.domain.com"
        /// </summary>
        Domain,

        /// <summary>
        ///     In john.doe+spam@first.domain.com, it's "first.domain"
        /// </summary>
        DomainMain,

        /// <summary>
        ///     In john.doe+spam@first.domain.com, it's "com"
        /// </summary>
        DomainSuffix,
    }

    /// <summary>
    ///     Smart class can be used a substitute for "string emailAddress;".
    ///     Has implicit conversion operators from and to string and thus can be used in method parameters for email addresses.
    /// </summary>
    public sealed class EmailAddress : StringWithConstraints
    {
        /// <summary>
        ///     Global email address format check regular expression pattern.
        ///     I suspect it will be continually improved and updated.
        /// </summary>
        public static readonly string EmailCheckRegexPattern =
            @"(?<UserBeforeAt> (?<UserBeforePlus> [^@\+]{2,} )  (?: \+ (?<UserAfterPlusFilter> [^@]{1,} )){0,1}  ) @  (?<Domain> (?<DomainMain>.{2,})  \. (?<DomainSuffix> \w{2,6}) )".Replace(" ", string.Empty);

        /// <summary>
        ///     Global email address format check regular expression.
        /// </summary>
        public static Regex EmailFormatRegex = new Regex(EmailCheckRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public Match Match { get; private set; }

        public EmailAddress() : this(null)
        {
        }

        public EmailAddress(string emailAddress) : base(str => str)
        {
            this.String = emailAddress;
        }

        public override string String
        {
            get { return this.FullAddress; }
            set { this.Match = ParseEmailAddress(value); }
        }

        /// <summary>
        ///     Returns true if parsed string was valid email address.
        /// </summary>
        public bool IsValid
        {
            get { return this.Match != null && this.Match.Success; }
        }

        public static implicit operator EmailAddress(string emailAddress)
        {
            return new EmailAddress(emailAddress);
        }

        /// <summary>
        ///     Returns null if parsed string was not of the valid email format.
        ///     Otherwise return a part of an email address.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public string this[EmailAddressParts part]
        {
            get { return this.Match.GetGroupValue(part.ToString()); }
        }

        /// <summary>
        ///     Returns full email address by rebuilding it from parsed parts.
        ///     Returns null if parsed string was not in the valid email format.
        /// </summary>
        public string FullAddress
        {
            get { return !this.IsValid ? null : "{0}@{1}".SmartFormat(this[EmailAddressParts.UserBeforeAt], this[EmailAddressParts.Domain]); }
        }

        /// <summary>
        ///     Returns email address without "+whatever" part.
        ///     For example, if source email address string was "johndoe+spam@doamin.com",
        ///     this property will return "johndoe@doamin.com".
        ///     Returns null if parsed string was not in the valid email format.
        /// </summary>
        public string AddressWithoutFilter
        {
            get { return !this.IsValid ? null : "{0}@{1}".SmartFormat(this[EmailAddressParts.UserBeforePlus], this[EmailAddressParts.Domain]); }
        }

        #region Utility methods

        public static Match ParseEmailAddress(string emailAddress)
        {
            if(emailAddress == null)
                emailAddress = string.Empty;

            return EmailFormatRegex.Match(emailAddress.Trim());
        }

        #endregion Utility methods
    }

    public static class EmailHelper
    {
        /// <summary>
        ///     Optional SMTP client factory that can add non-configuration credentials, etc.
        /// </summary>
        public static Func<SmtpClient> SmtpClientFactory = null;

        /// <summary>
        ///     Sends SMTP email using .config file settings.
        /// </summary>
        /// <param name="isBodyHtml"></param>
        /// <param name="optioanlFromAddress">If null, .config from address value is used.</param>
        /// <param name="optionalReplyToAddress">If null, reply-to address is the same as from address.</param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="toAddresses"></param>
        public static void SendSmtpEmail(bool isBodyHtml, NonEmptyString optioanlFromAddress, NonEmptyString optionalReplyToAddress, string subject, string body, params string[] toAddresses)
        {
            if(toAddresses != null)
                toAddresses = toAddresses.Where(addr => !addr.IsBlank()).ToArray();

            if(toAddresses.IsNullOrEmpty())
                throw new Exception("\"To\" address must be specified");

            if(subject.IsBlank() && body.IsBlank())
                throw new Exception("Both subject and message body cannot be blank.");

            MailMessage message = new MailMessage();

            if(optioanlFromAddress != null)
                message.From = new MailAddress(optioanlFromAddress);

            if(optionalReplyToAddress != null)
                message.ReplyToList.Add(new MailAddress(optionalReplyToAddress));

            toAddresses.ForEach(toAddr => message.To.Add(new MailAddress(toAddr)));

            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isBodyHtml;

            SmtpClient smtpClient = SmtpClientFactory == null ? null : SmtpClientFactory();
            if(smtpClient == null)
                smtpClient = new SmtpClient();

            using(smtpClient)
            {
                smtpClient.Send(message);
            }
        }

        /// <summary>
        ///     Returns true if text matches email address regular expression pattern.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public static bool IsValidEmailFormat(this string emailAddress)
        {
            if(emailAddress == null)
                return false;

            return EmailAddress.EmailFormatRegex.IsMatch(emailAddress);
        }

        /// <summary>
        ///     Same as EmailAddress, but handles null gracefully. Returns false if email is null.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsValid(this EmailAddress email)
        {
            return email != null && email.IsValid;
        }
    }
}