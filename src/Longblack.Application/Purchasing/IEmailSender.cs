namespace Longblack.Application.Purchasing;

public record EmailAttachment(string FileName, string ContentType, byte[] Content);

public record EmailMessage(
    string To,
    string? Cc,
    string Subject,
    string Body,
    IReadOnlyList<EmailAttachment> Attachments);

public interface IEmailSender
{
    // Throws on delivery failure; callers are responsible for logging the outcome.
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
