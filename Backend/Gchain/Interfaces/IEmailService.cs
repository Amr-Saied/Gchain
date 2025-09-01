using Gchain.Models;

namespace Gchain.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a badge achievement email
    /// </summary>
    Task<bool> SendBadgeAchievementEmailAsync(
        string userEmail,
        string userName,
        Badge badge,
        string? reason = null
    );

    /// <summary>
    /// Sends a general email
    /// </summary>
    Task<bool> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        bool isHtml = true
    );

    /// <summary>
    /// Sends a welcome email to new users
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string userEmail, string userName);

    /// <summary>
    /// Tests email configuration
    /// </summary>
    Task<bool> TestEmailConfigurationAsync();

    /// <summary>
    /// Gets whether email service is enabled
    /// </summary>
    bool IsEnabled { get; }
}
