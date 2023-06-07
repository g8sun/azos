/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Linq;

using System.Net;
using System.Net.Mail;

using Azos.Conf;
using Azos.Log;
using Azos.Web;

namespace Azos.Sky.Messaging.Sinks
{
  /// <summary>
  /// Implements msg sink based on SMTPClient
  /// </summary>
  public class SMTPMessageSink : MessageSink
  {
    #region CONSTS

    public const int DEFAULT_SMTP_PORT = 587;

    #endregion

    #region .ctor

    public SMTPMessageSink(MessageDaemon director) : base(director)
    {
      SmtpPort = DEFAULT_SMTP_PORT;
    }

    protected override void Destructor()
    {
      base.Destructor();
    }

    #endregion

    #region Private Fields

    private SmtpClient m_Smtp;

    #endregion

    #region Properties

      [Config]
      public string SmtpHost { get; set; }

      [Config(Default=DEFAULT_SMTP_PORT)]
      public int SmtpPort { get; set; }

      [Config]
      public bool SmtpSSL { get; set; }

      [Config]
      public string CredentialsID { get; set; }

      [Config]
      public string CredentialsPassword { get; set; }

      [Config]
      public string DefaultFromAddress { get; set; }

      [Config]
      public string DefaultFromName { get; set; }

      [Config]
      public string DropFolder { get; set; }

      public override MsgChannels SupportedChannels
      {
        get
        {
          return MsgChannels.EMail;
        }
      }

    #endregion

    #region Protected

    protected override void DoStart()
    {
      m_Smtp = new SmtpClient();

      if (DropFolder.IsNotNullOrWhiteSpace() && System.IO.Directory.Exists(DropFolder))
      {
        m_Smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
        m_Smtp.PickupDirectoryLocation = DropFolder;
      }
      else
      {
        if (SmtpHost.IsNullOrWhiteSpace())
        throw new SkyException(StringConsts.MAILER_SINK_SMTP_IS_NOT_CONFIGURED_ERROR+"SmtpHost==null|empty|0");

        m_Smtp.Host = this.SmtpHost;
        m_Smtp.Port = this.SmtpPort;
        m_Smtp.EnableSsl = this.SmtpSSL;
        m_Smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
        m_Smtp.UseDefaultCredentials = false;
        m_Smtp.Credentials = new NetworkCredential(this.CredentialsID, this.CredentialsPassword);
      }
    }

    protected override void DoWaitForCompleteStop()
    {
      if (m_Smtp != null)
      {
        m_Smtp.Dispose();
        m_Smtp = null;
      }
    }

    protected override bool DoSendMsg(Message msg)
    {
      // If msg is null or msg hasn't any "To" addressee - we can't send it, so return
      if (msg == null || !msg.AddressToBuilder.All.Any()) return false;

      var addressFrom = msg.AddressFromBuilder.GetFirstOrDefaultMatchForChannels(SupportedChannelNames);

      var addressTo = msg.AddressToBuilder.GetMatchesForChannels(SupportedChannelNames).ToList();

      var addressReplyTo = msg.AddressReplyToBuilder.GetMatchesForChannels(SupportedChannelNames);
      var arto = addressReplyTo.Select(fmtEmail).ToList();

      var addressCC = msg.AddressCCBuilder.GetMatchesForChannels(SupportedChannelNames);
      var acc = addressCC.Select(fmtEmail).ToList();

      var addressBCC = msg.AddressBCCBuilder.GetMatchesForChannels(SupportedChannelNames);
      var abcc = addressBCC.Select(fmtEmail).ToList();

      var fa = addressFrom.Address;
      var fn = addressFrom.Channel;

      if (fa.IsNullOrWhiteSpace()) fa = DefaultFromAddress;
      if (fn.IsNullOrWhiteSpace()) fn = DefaultFromName;

      var from = fmtEmail(fa, fn);
      var wasSent = false;

      foreach (var addressee in addressTo)
      {
        var to = fmtEmail(addressee.Address, addressee.Name);

        using (var email = new MailMessage(from, to))
        {
          foreach (var adr in arto)
            email.ReplyToList.Add(adr);

          foreach (var adr in acc)
            email.CC.Add(adr);

          foreach (var adr in abcc)
            email.Bcc.Add(adr);

          email.Subject = msg.Subject;

          if (msg.RichBody.IsNullOrWhiteSpace())
          {
            email.Body = msg.Body;
          }
          else
          {
            if (msg.Body.IsNullOrWhiteSpace())
            {
              email.IsBodyHtml = true;
              email.Body = msg.RichBody;
            }
            else
            {
              email.Body = msg.Body;
              var alternateHTML = AlternateView.CreateAlternateViewFromString(msg.RichBody, new System.Net.Mime.ContentType(ContentType.HTML));
              email.AlternateViews.Add(alternateHTML);
            }
          }

          if (msg.Attachments != null)
            foreach (var att in msg.Attachments.Where(a => a.Content != null))
            {
              var ema = new Attachment(new System.IO.MemoryStream(att.Content), new System.Net.Mime.ContentType(att.ContentType));

              if (att.Name.IsNotNullOrWhiteSpace())
                ema.ContentDisposition.FileName = att.Name;

              email.Attachments.Add(ema);
            }

          try
          {
            m_Smtp.Send(email);
            wasSent = true;
          }
          catch (Exception error)
          {
            WriteLog(MessageType.Error, nameof(DoSendMsg), error.ToMessageWithType(), error);
          }
        }
      }

      return wasSent;
    }

    #endregion

    private MailAddress fmtEmail(MessageAddressBuilder.Addressee addressee)
    {
      return fmtEmail(addressee.Address, addressee.Name);
    }

    private MailAddress fmtEmail(string address, string name)
    {
      return new MailAddress(address, name);
    }
  }
}
