using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services;

public sealed class PonyUpIdentityEmailSender :
    IEmailSender<ApplicationUser>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PonyUpIdentityEmailSender> _logger;

    public PonyUpIdentityEmailSender(
        IConfiguration configuration,
        ILogger<PonyUpIdentityEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink)
    {
        return SendAsync(
            email,
            "Confirm your PonyUp Performance email",
            $"<p>Confirm your PonyUp Performance email by <a href=\"{confirmationLink}\">clicking here</a>.</p>");
    }

    public Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink)
    {
        return SendAsync(
            email,
            "Reset your PonyUp Performance password",
            $"<p>Reset your PonyUp Performance password by <a href=\"{resetLink}\">clicking here</a>.</p>");
    }

    public Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode)
    {
        return SendAsync(
            email,
            "Your PonyUp Performance password reset code",
            $"<p>Your PonyUp Performance password reset code is <strong>{WebUtility.HtmlEncode(resetCode)}</strong>.</p>");
    }

    private async Task SendAsync(
        string recipient,
        string subject,
        string htmlBody)
    {
        string host =
            _configuration["Email:SmtpHost"]
            ?? string.Empty;

        string username =
            _configuration["Email:Username"]
            ?? string.Empty;

        string password =
            _configuration["Email:Password"]
            ?? string.Empty;

        string fromAddress =
            _configuration["Email:FromAddress"]
            ?? username;

        string fromName =
            _configuration["Email:FromName"]
            ?? "PonyUp Performance";

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogError(
                "Identity email delivery was requested but SMTP is not fully configured.");

            throw new InvalidOperationException(
                "PonyUp email delivery is not configured.");
        }

        int port =
            int.TryParse(
                _configuration["Email:SmtpPort"],
                out int configuredPort)
                ? configuredPort
                : 587;

        bool enableSsl =
            !bool.TryParse(
                _configuration["Email:EnableSsl"],
                out bool configuredSsl) ||
            configuredSsl;

        using var message =
            new MailMessage
            {
                From =
                    new MailAddress(
                        fromAddress,
                        fromName),

                Subject =
                    subject,

                Body =
                    htmlBody,

                IsBodyHtml =
                    true
            };

        message.To.Add(
            new MailAddress(
                recipient));

        using var client =
            new SmtpClient(
                host,
                port)
            {
                EnableSsl =
                    enableSsl,

                Credentials =
                    new NetworkCredential(
                        username,
                        password)
            };

        await client.SendMailAsync(
            message);
    }
}
