using Microsoft.AspNetCore.Http;

namespace Gchain.DTOS;

/// <summary>
/// Request model for updating user profile
/// </summary>
public class UpdateUserProfileRequest
{
    /// <summary>
    /// New username (optional)
    /// </summary>
    public string? NewUserName { get; set; }

    /// <summary>
    /// Profile picture file (optional)
    /// </summary>
    public IFormFile? ProfilePicture { get; set; }
}
