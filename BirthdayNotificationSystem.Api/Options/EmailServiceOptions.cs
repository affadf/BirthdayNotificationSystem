namespace BirthdayNotificationSystem.Api.Options;

/// <summary>
/// Configuration values for the external email service integration.
/// </summary>
public sealed class EmailServiceOptions
{
    /// <summary>
    /// Configuration section name used by the options binder.
    /// </summary>
    public const string SectionName = "EmailService";

    /// <summary>
    /// Base URL for the external email service.
    /// </summary>
    public string BaseUrl { get; set; } = "https://email-service.digitalenvision.com.au";

    /// <summary>
    /// Relative path for the send-mail endpoint.
    /// </summary>
    public string SendMailPath { get; set; } = "/api/v1/send-mail";

    /// <summary>
    /// Authorization header value used when calling the email service.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Sender email address included in outbound notification messages.
    /// </summary>
    public string FromEmail { get; set; } = "birthdays@example.com";

    /// <summary>
    /// Subject line used for birthday messages.
    /// </summary>
    public string BirthdaySubject { get; set; } = "Happy birthday";

    /// <summary>
    /// Subject line used for anniversary messages.
    /// </summary>
    public string AnniversarySubject { get; set; } = "Happy anniversary";

    /// <summary>
    /// Fallback subject line for notification types without a specific configured subject.
    /// </summary>
    public string DefaultSubject { get; set; } = "Notification";

    /// <summary>
    /// Timeout in seconds for each email service attempt.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}
