using System.Net;
using System.Net.Mail;
using KambaDiami.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KambaDiami.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>Vazio usa o próprio Username (o Gmail não aceita outro remetente).</summary>
    public string? FromAddress { get; set; }
    public string FromName { get; set; } = "Kamba Diami";

    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }

    public bool IsSmtpConfigured =>
        !string.IsNullOrWhiteSpace(SmtpHost)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password);

    public string EffectiveFromAddress =>
        string.IsNullOrWhiteSpace(FromAddress) ? Username ?? string.Empty : FromAddress;
}

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public SmtpEmailSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.EffectiveFromAddress, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = string.IsNullOrEmpty(_settings.Username)
                ? null
                : new NetworkCredential(_settings.Username, _settings.Password)
        };

        await client.SendMailAsync(message, ct);
    }
}

/// <summary>
/// Usado quando não há servidor SMTP configurado (por exemplo, em desenvolvimento):
/// o email é escrito no log em vez de ser enviado.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "SMTP não configurado. Email para {To} não foi enviado.\nAssunto: {Subject}\n{Body}",
            toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
