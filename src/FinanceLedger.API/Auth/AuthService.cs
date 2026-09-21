using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;
using Microsoft.AspNetCore.Authentication;

namespace FinanceLedger.API.Auth;

public interface ILoginService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<MeResponse> GetMeAsync(CancellationToken ct = default);
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
    private readonly ITenantRepository _tenantRepository;
    private readonly IIdentityProviderService _identityProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUser;

    public AuthService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IIdentityProviderService identityProvider,
        IHttpContextAccessor httpContextAccessor,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _identityProvider = identityProvider;
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
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

        // The caller has no site yet (that is what login establishes), so look across sites.
        var user = await _userRepository.GetByIdAnyTenantAsync(grant.UserId, ct);
        var tenant = user is null ? null : await GetActiveTenantAsync(user, ct);
        if (user is null || !user.IsActive || (user.Role != UserRole.AppAdmin && tenant is null))
        {
            throw new ForbiddenException("Invalid credentials.");
        }

        return ToLoginResponse(grant, user, tenant);
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

        var user = await _userRepository.GetByIdAnyTenantAsync(grant.UserId, ct);
        var tenant = user is null ? null : await GetActiveTenantAsync(user, ct);
        if (user is null || !user.IsActive || (user.Role != UserRole.AppAdmin && tenant is null))
        {
            throw new ForbiddenException("Invalid or expired refresh token.");
        }

        return ToLoginResponse(grant, user, tenant);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var accessToken = await _httpContextAccessor.HttpContext!.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            await _identityProvider.RevokeAsync(accessToken, ct);
        }
    }

    /// <summary>
    /// Strictly self-service: a caller may only reset their own password, and must
    /// always prove they know the current one. (An earlier version of this method
    /// only required CurrentPassword "if provided" — since ResetPasswordRequest.Email
    /// was never checked against the caller's identity, that let any authenticated
    /// user reset any other user's password by simply omitting CurrentPassword. A
    /// Manager resetting someone else's forgotten password goes through the separate
    /// POST /users/{id}/reset-password admin endpoint instead, which is authorized by
    /// role rather than by proving the old password.)
    /// </summary>
    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        if (_currentUser.IsActingAs)
        {
            throw new ForbiddenException("Exit \"View as\" before changing a password.");
        }

        if (!string.Equals(request.Email, _currentUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("You can only reset your own password.");
        }

        var user = await _userRepository.GetByEmailAnyTenantAsync(request.Email, ct);
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

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                [nameof(request.CurrentPassword)] = new[] { "Your current password is required." }
            });
        }

        try
        {
            await _identityProvider.PasswordGrantAsync(request.Email, request.CurrentPassword, ct);
        }
        catch (IdentityProviderException)
        {
            throw new ForbiddenException("Current password is incorrect.");
        }

        await _identityProvider.AdminUpdateUserAsync(user.Id, password: request.NewPassword, ct: ct);
    }

    public async Task<MeResponse> GetMeAsync(CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAnyTenantAsync(_currentUser.UserId ?? string.Empty, ct)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId ?? string.Empty);
        var tenant = await GetActiveTenantAsync(user, ct);

        return new MeResponse
        {
            User = ToUserDto(user, tenant),
            ActingAs = _currentUser.IsActingAs
                ? new ActingAsDto
                {
                    RealUserId = _currentUser.ActingAdminId ?? string.Empty,
                    RealUserName = _currentUser.ActingAdminName ?? string.Empty,
                    CanWrite = _currentUser.ActingCanWrite
                }
                : null
        };
    }

    private async Task<Tenant?> GetActiveTenantAsync(User user, CancellationToken ct)
    {
        if (user.Role == UserRole.AppAdmin || string.IsNullOrEmpty(user.TenantId))
        {
            return null;
        }

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, ct);
        return tenant is { IsActive: true } ? tenant : null;
    }

    private static UserDto ToUserDto(User user, Tenant? tenant)
    {
        var dto = user.ToDto();
        dto.TenantName = tenant?.Name;
        return dto;
    }

    private static LoginResponse ToLoginResponse(AuthGrantResult grant, User user, Tenant? tenant) => new()
    {
        AccessToken = grant.AccessToken,
        RefreshToken = grant.RefreshToken,
        ExpiresAt = grant.ExpiresAt,
        TokenType = "Bearer",
        User = ToUserDto(user, tenant)
    };
}
