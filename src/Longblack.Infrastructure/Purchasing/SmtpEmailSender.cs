using System.Net;
using System.Net.Mail;
using Longblack.Application.Purchasing;
using Microsoft.Extensions.Options;

namespace Longblack.Infrastructure.Purchasing;

public class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("Email is not configured. Set Email:Host and Email:FromAddress.");

        using var mail = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = message.Subject,
            Body = message.Body
        };
        mail.To.Add(message.To);
        if (!string.IsNullOrWhiteSpace(message.Cc))
            mail.CC.Add(message.Cc);

        var streams = new List<MemoryStream>();
        try
        {
            foreach (var a in message.Attachments)
            {
                var stream = new MemoryStream(a.Content);
                streams.Add(stream);
                mail.Attachments.Add(new Attachment(stream, a.FileName, a.ContentType));
            }

            using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
            if (!string.IsNullOrWhiteSpace(settings.Username))
                client.Credentials = new NetworkCredential(settings.Username, settings.Password);

            await client.SendMailAsync(mail, ct);
        }
        finally
        {
            foreach (var s in streams)
                s.Dispose();
        }
    }
}
