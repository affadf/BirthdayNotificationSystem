using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BirthdayNotificationSystem.Api.Domain;
using BirthdayNotificationSystem.Api.Options;
using Microsoft.Extensions.Options;

namespace BirthdayNotificationSystem.Api.Services;

/// <summary>
/// Sends notification messages through the configured Digital Envision email service endpoint.
/// </summary>
public sealed class EmailServiceMessageSender(
    HttpClient httpClient,
    IOptions<EmailServiceOptions> options,
    ILogger<EmailServiceMessageSender> logger) : IMessageSender
{
    private readonly EmailServiceOptions _options = options.Value;

    /// <summary>
    /// Sends a notification message with short transient retries and an idempotency key for duplicate mitigation.
    /// </summary>
    /// <param name="user">The recipient user.</param>
    /// <param name="notification">The notification being delivered.</param>
    /// <param name="message">The rendered notification message body.</param>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    /// <exception cref="InvalidOperationException">Thrown when all transient send attempts fail.</exception>
    public async Task SendMessageAsync(
        User user,
        Notification notification,
        string message,
        CancellationToken cancellationToken)
    {
        var request = new SendMailRequest(
            user.Email,
            _options.FromEmail,
            GetSubject(notification.NotificationType),
            message);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.SendMailPath)
                {
                    Content = JsonContent.Create(request)
                };

                if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    httpRequest.Headers.TryAddWithoutValidation("Authorization", _options.ApiKey);
                }

                httpRequest.Headers.TryAddWithoutValidation(
                    "Idempotency-Key",
                    $"{notification.NotificationType.ToString().ToLowerInvariant()}-{user.Id}-{notification.EventYear}");

                using var response = await httpClient.SendAsync(httpRequest, linked.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                var responseBody = await response.Content.ReadAsStringAsync(linked.Token);
                lastException = new InvalidOperationException(
                    $"Email service returned {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(responseBody, 500)}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
            {
                lastException = ex;
            }

            if (attempt < 3)
            {
                logger.LogWarning(
                    lastException,
                    "Transient notification email send failure. Attempt {Attempt} of 3.",
                    attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException("Email service send failed after transient retries.", lastException);
    }

    private string GetSubject(NotificationType notificationType)
    {
        return notificationType switch
        {
            NotificationType.Birthday => _options.BirthdaySubject,
            NotificationType.Anniversary => _options.AnniversarySubject,
            _ => _options.DefaultSubject
        };
    }

    /// <summary>
    /// Trims long response bodies before storing them as error details.
    /// </summary>
    /// <param name="value">The text to trim.</param>
    /// <param name="maxLength">The maximum number of characters to keep.</param>
    /// <returns>The original text or its trimmed prefix.</returns>
    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    /// <summary>
    /// Request body sent to the external email service.
    /// </summary>
    private sealed record SendMailRequest(
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("message")] string Message);
}
