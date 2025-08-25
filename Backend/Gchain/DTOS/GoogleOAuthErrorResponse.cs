namespace Gchain.DTOS;

/// <summary>
/// Error response model for Google OAuth authentication failures
/// </summary>
public class GoogleOAuthErrorResponse
{
    /// <summary>
    /// Error code
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error description
    /// </summary>
    public string ErrorDescription { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details for debugging
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Timestamp when error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
