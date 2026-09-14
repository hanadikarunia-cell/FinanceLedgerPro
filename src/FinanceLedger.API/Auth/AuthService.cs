using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using Microsoft.AspNetCore.Authentication;

namespace FinanceLedger.API.Auth;

public interface ILoginService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}

/// <summary>
/// Bridges the API's stable auth contract (unchanged since the Azure/Entra days) to
/// Supabase Auth: credential verification, session issuance, and refresh-token
/// rotation are all delegated to Supabase; this class only maps the request/response
/// shapes and fills in domain fields (display name, role, branches) Supabase doesn't
/// know about from the local `users` table.
/// </summary>
public class AuthService : ILoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityProviderService _identityProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IUserRepository userRepository,
        IIdentityProviderService identityProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _identityProvider = identityProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        AuthGrantResult grant;
        try
        {
            grant = await _identityProvider.PasswordGrantAsync(request.Email, request.Password, ct);
        }
        catch (IdentityProviderException)
        {
            throw new ForbiddenException("Invalid credentials.");
        }

        var user = await _userRepository.GetByIdAsync(grant.UserId, ct);
        if (user is null || !user.IsActive)
        {
            throw new ForbiddenException("Invalid credentials.");
        }

        return ToLoginResponse(grant, user);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        AuthGrantResult grant;
        try
        {
            grant = await _identityProvider.RefreshGrantAsync(request.RefreshToken, ct);
        }
        catch (IdentityProviderException)
        {
            throw new ForbiddenException("Invalid or expired refresh token.");
        }

        var user = await _userRepository.GetByIdAsync(grant.UserId, ct);
        if (user is null || !user.IsActive)
        {
            throw new ForbiddenException("Invalid or expired refresh token.");
        }

        return ToLoginResponse(grant, user);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var accessToken = await _httpContextAccessor.HttpContext!.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            await _identityProvider.RevokeAsync(accessToken, ct);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive)
        {
            throw new NotFoundException("User", request.Email);
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                [nameof(request.NewPassword)] = new[] { "A new password is required." }
            });
        }

        if (!string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            try
            {
                await _identityProvider.PasswordGrantAsync(request.Email, request.CurrentPassword, ct);
            }
            catch (IdentityProviderException)
            {
                throw new ForbiddenException("Current password is incorrect.");
            }
        }

        await _identityProvider.AdminUpdateUserAsync(user.Id, password: request.NewPassword, ct: ct);
    }

    private static LoginResponse ToLoginResponse(AuthGrantResult grant, User user) => new()
    {
        AccessToken = grant.AccessToken,
        RefreshToken = grant.RefreshToken,
        ExpiresAt = grant.ExpiresAt,
        TokenType = "Bearer",
        User = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            AssignedBranches = user.AssignedBranches,
            IsActive = user.IsActive,
            CreatedDate = user.CreatedDate
        }
    };
}
