using System.Security.Claims;
using FinanceLedger.API.Services;
using FinanceLedger.Application.Common;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.API.Middleware;

/// <summary>
/// Turns a validated token into the effective identity for the request, from the
/// database rather than from token claims:
///  - the caller's role, site and branches are always the current stored values, so a
///    deactivated user, a deactivated site or a changed role takes effect immediately;
///  - the client can never choose its own site: there is no site claim in the token;
///  - "View as": an admin may send X-Act-As-User (and X-Act-As-Write: true to allow
///    changes). The request then runs as that user, with their site, role and
///    branches, while the real admin stays on record for auditing. Without the write
///    header the session is read-only.
/// Runs after authentication and before authorization, so role policies see the
/// effective role.
/// </summary>
public class UserContextMiddleware
{
    public const string ActAsUserHeader = "X-Act-As-User";
    public const string ActAsWriteHeader = "X-Act-As-Write";

    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository users, ITenantRepository tenants)
    {
        var subject = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirstValue("sub")
            : null;

        // Login/refresh establish an identity, so a leftover token must not interfere.
        var path = context.Request.Path.Value ?? string.Empty;
        var establishesSession = path.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(subject) || establishesSession)
        {
            await _next(context);
            return;
        }

        var ct = context.RequestAborted;

        var real = await users.GetByIdAnyTenantAsync(subject, ct);
        if (real is null || !real.IsActive)
            throw new ForbiddenException("This account is not active.");

        var effective = real;
        var effectiveTenant = await LoadActiveTenantAsync(real, tenants, ct);

        var actAsId = context.Request.Headers[ActAsUserHeader].FirstOrDefault();
        var acting = !string.IsNullOrWhiteSpace(actAsId);
        var canWrite = false;

        if (acting)
        {
            if (real.Role == UserRole.User)
                throw new ForbiddenException("Only admins can use \"View as\".");

            var target = await users.GetByIdAnyTenantAsync(actAsId!, ct)
                ?? throw new NotFoundException(nameof(User), actAsId!);

            if (target.Id == real.Id)
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    [ActAsUserHeader] = new[] { "You cannot view as yourself." }
                });

            // The Application Admin may act as anyone in any site (never as another
            // Application Admin); a Site Admin only as regular users of their own site.
            var allowed = real.Role == UserRole.AppAdmin
                ? target.Role != UserRole.AppAdmin
                : target.TenantId == real.TenantId && target.Role == UserRole.User;
            if (!allowed)
                throw new ForbiddenException("You cannot view as that user.");

            if (!target.IsActive)
                throw new ForbiddenException("That user is deactivated.");

            effective = target;
            effectiveTenant = await LoadActiveTenantAsync(target, tenants, ct);
            canWrite = string.Equals(
                context.Request.Headers[ActAsWriteHeader].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase);

            if (!canWrite && !IsReadOnlySafe(context.Request))
                throw new ForbiddenException(
                    "You are viewing as another user in read-only mode. Turn on \"Allow changes\" to make changes.");
        }

        context.User = BuildPrincipal(effective, effectiveTenant, acting ? real : null, canWrite);
        await _next(context);
    }

    private static async Task<Tenant?> LoadActiveTenantAsync(User user, ITenantRepository tenants, CancellationToken ct)
    {
        if (user.Role == UserRole.AppAdmin)
            return null;

        var tenant = string.IsNullOrEmpty(user.TenantId) ? null : await tenants.GetByIdAsync(user.TenantId, ct);
        if (tenant is not { IsActive: true })
            throw new ForbiddenException("This site is not active.");

        return tenant;
    }

    /// <summary>Reads, plus the POST endpoints that only read (exports) or manage the session.</summary>
    private static bool IsReadOnlySafe(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
            return true;

        var path = request.Path.Value ?? string.Empty;
        return path.Contains("/export/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/auth/", StringComparison.OrdinalIgnoreCase);
    }

    private static ClaimsPrincipal BuildPrincipal(User effective, Tenant? tenant, User? realAdmin, bool canWrite)
    {
        var claims = new List<Claim>
        {
            new("sub", effective.Id),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(effective.DisplayName) ? effective.Email : effective.DisplayName),
            new(ClaimTypes.Email, effective.Email),
            new(ClaimTypes.Role, effective.Role.ToString())
        };

        claims.AddRange(effective.AssignedBranches.Select(b => new Claim("branches", b)));

        if (tenant is not null)
            claims.Add(new Claim(TenantClaims.TenantId, tenant.Id));

        if (realAdmin is not null)
        {
            claims.Add(new Claim(TenantClaims.ActingAdminId, realAdmin.Id));
            claims.Add(new Claim(TenantClaims.ActingAdminName,
                string.IsNullOrWhiteSpace(realAdmin.DisplayName) ? realAdmin.Email : realAdmin.DisplayName));
            claims.Add(new Claim(TenantClaims.ActingCanWrite, canWrite ? "true" : "false"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "UserContext", ClaimTypes.Name, ClaimTypes.Role));
    }
}
