using System.ComponentModel.DataAnnotations;

namespace Gchain.DTOS;

/// <summary>
/// Request model for Google OAuth authentication
/// </summary>
public class GoogleOAuthRequest
{
    /// <summary>
    /// Google OAuth authorization code or ID token
    /// </summary>
    [Required(ErrorMessage = "Token is required")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Type of token being provided (code or id_token)
    /// </summary>
    [Required(ErrorMessage = "Token type is required")]
    public string TokenType { get; set; } = "id_token"; // "code" or "id_token"
}
