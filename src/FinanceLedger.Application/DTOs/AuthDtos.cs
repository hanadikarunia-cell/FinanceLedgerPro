namespace FinanceLedger.Application.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public UserDto User { get; set; } = new();
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Set when an admin is "acting as" another user; describes the real admin.</summary>
public class ActingAsDto
{
    public string RealUserId { get; set; } = string.Empty;
    public string RealUserName { get; set; } = string.Empty;
    public bool CanWrite { get; set; }
}

/// <summary>The effective identity for this request (the acted-as user, if any).</summary>
public class MeResponse
{
    public UserDto User { get; set; } = new();
    public ActingAsDto? ActingAs { get; set; }
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}
