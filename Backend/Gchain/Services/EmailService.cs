using System.Net;
using System.Net.Mail;
using Gchain.Interfaces;
using Gchain.Models;
using Microsoft.Extensions.Options;

namespace Gchain.Services;

/// <summary>
/// Service for sending emails
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public bool IsEnabled => _settings.IsEnabled;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendBadgeAchievementEmailAsync(
        string userEmail,
        string userName,
        Badge badge,
        string? reason = null
    )
    {
        if (!IsEnabled)
        {
            _logger.LogDebug(
                "Email service is disabled, skipping badge achievement email to {UserEmail}",
                userEmail
            );
            return false;
        }

        try
        {
            var subject = $"🏆 Congratulations! You earned the '{badge.Name}' badge!";
            var body = GenerateBadgeAchievementEmailBody(userName, badge, reason);

            return await SendEmailAsync(userEmail, userName, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send badge achievement email to {UserEmail}",
                userEmail
            );
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        bool isHtml = true
    )
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Email service is disabled, skipping email to {ToEmail}", toEmail);
            return false;
        }

        var retryCount = 0;
        var maxRetries = _settings.MaxRetryAttempts;

        while (retryCount <= maxRetries)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var message = CreateMailMessage(toEmail, toName, subject, body, isHtml);

                await client.SendMailAsync(message);

                _logger.LogInformation(
                    "Email sent successfully to {ToEmail} with subject: {Subject}",
                    toEmail,
                    subject
                );
                return true;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(
                    ex,
                    "Failed to send email to {ToEmail} (attempt {Attempt}/{MaxAttempts})",
                    toEmail,
                    retryCount,
                    maxRetries + 1
                );

                if (retryCount <= maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds));
                }
                else
                {
                    _logger.LogError(
                        ex,
                        "Failed to send email to {ToEmail} after {MaxAttempts} attempts",
                        toEmail,
                        maxRetries + 1
                    );
                    return false;
                }
            }
        }

        return false;
    }

    public async Task<bool> SendWelcomeEmailAsync(string userEmail, string userName)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug(
                "Email service is disabled, skipping welcome email to {UserEmail}",
                userEmail
            );
            return false;
        }

        try
        {
            var subject = "🎉 Welcome to Gchain!";
            var body = GenerateWelcomeEmailBody(userName);

            return await SendEmailAsync(userEmail, userName, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {UserEmail}", userEmail);
            return false;
        }
    }

    public async Task<bool> TestEmailConfigurationAsync()
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Email service is disabled");
            return false;
        }

        try
        {
            using var client = CreateSmtpClient();

            // Test connection
            await client.SendMailAsync(
                new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    To = { _settings.FromEmail }, // Send test email to self
                    Subject = "Gchain Email Service Test",
                    Body = "This is a test email to verify email configuration.",
                    IsBodyHtml = false
                }
            );

            _logger.LogInformation("Email configuration test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email configuration test failed");
            return false;
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
            EnableSsl = _settings.UseSsl,
            Timeout = 30000 // 30 seconds timeout
        };

        return client;
    }

    private MailMessage CreateMailMessage(
        string toEmail,
        string toName,
        string subject,
        string body,
        bool isHtml
    )
    {
        var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        message.To.Add(new MailAddress(toEmail, toName));

        return message;
    }

    private string GenerateBadgeAchievementEmailBody(string userName, Badge badge, string? reason)
    {
        var reasonText = !string.IsNullOrEmpty(reason)
            ? $"<p><strong>Reason:</strong> {reason}</p>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Badge Achievement - Gchain</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .badge {{ background: #fff; border: 2px solid #667eea; border-radius: 10px; padding: 20px; margin: 20px 0; text-align: center; }}
        .badge-icon {{ font-size: 48px; margin-bottom: 10px; }}
        .badge-name {{ font-size: 24px; font-weight: bold; color: #667eea; margin-bottom: 10px; }}
        .badge-description {{ color: #666; font-size: 16px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🏆 Badge Achievement!</h1>
        <p>Congratulations on your accomplishment!</p>
    </div>
    
    <div class='content'>
        <h2>Hello {userName}!</h2>
        
        <p>We're excited to let you know that you've earned a new badge in Gchain!</p>
        
        <div class='badge'>
            <div class='badge-icon'>🏅</div>
            <div class='badge-name'>{badge.Name}</div>
            <div class='badge-description'>{badge.Description}</div>
        </div>
        
        {reasonText}
        
        <p>Keep playing to earn more badges and climb the leaderboard!</p>
        
        <div style='text-align: center;'>
            <a href='#' class='button'>Play Gchain Now</a>
        </div>
    </div>
    
    <div class='footer'>
        <p>This email was sent by Gchain. If you have any questions, please contact our support team.</p>
        <p>© 2025 Gchain. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    private string GenerateWelcomeEmailBody(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome to Gchain</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .feature {{ margin: 20px 0; padding: 15px; background: white; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🎉 Welcome to Gchain!</h1>
        <p>Your word association adventure begins now!</p>
    </div>
    
    <div class='content'>
        <h2>Hello {userName}!</h2>
        
        <p>Welcome to Gchain, the exciting multiplayer word association game! We're thrilled to have you join our community.</p>
        
        <div class='feature'>
            <h3>🎮 How to Play</h3>
            <p>Join teams, guess words, and compete in best-of-3 rounds. Use the dice revival system to get back in the game!</p>
        </div>
        
        <div class='feature'>
            <h3>🏆 Earn Badges</h3>
            <p>Complete achievements, reach milestones, and unlock special badges as you play!</p>
        </div>
        
        <div class='feature'>
            <h3>📊 Climb the Leaderboard</h3>
            <p>Compete with players worldwide and see how you rank in different categories!</p>
        </div>
        
        <div style='text-align: center;'>
            <a href='#' class='button'>Start Playing Now</a>
        </div>
        
        <p>Have fun and good luck with your first game!</p>
    </div>
    
    <div class='footer'>
        <p>This email was sent by Gchain. If you have any questions, please contact our support team.</p>
        <p>© 2025 Gchain. All rights reserved.</p>
    </div>
</body>
</html>";
    }
}
