namespace FinanceLedger.Domain.Enums;

public enum AuditAction
{
    Create,
    Update,
    Delete,
    Approve,
    Reject,
    Void,

    // Security and administration events (recorded from Phase 2 onward).
    Login,
    Logout,
    PasswordReset,
    RoleChange,
    Deactivate,
    TenantCreate,
    TenantUpdate,
    ImpersonationStart,
    ImpersonationEnd,
    Export
}
