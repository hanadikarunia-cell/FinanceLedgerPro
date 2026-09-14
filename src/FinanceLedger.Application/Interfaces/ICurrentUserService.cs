using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    UserRole? Role { get; }
    IReadOnlyCollection<string> AssignedBranches { get; }
    bool IsManager { get; }
    bool IsAuthenticated { get; }
}
