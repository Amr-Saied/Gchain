namespace Gchain.DTOS;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
