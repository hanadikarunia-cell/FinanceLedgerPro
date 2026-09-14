using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Entities;

namespace FinanceLedger.Application.Common;

/// <summary>Lightweight hand-rolled mappers (no AutoMapper dependency).</summary>
public static class Mapping
{
    public static TransactionDto ToDto(this Transaction t) => new()
    {
        Id = t.Id,
        Type = t.Type,
        Category = t.Category,
        Description = t.Description,
        Amount = t.Amount,
        Date = t.Date,
        Branch = t.Branch,
        CreatedBy = t.CreatedBy,
        CreatedByName = t.CreatedByName,
        CreatedDate = t.CreatedDate,
        ApprovalStatus = t.ApprovalStatus,
        ApprovedBy = t.ApprovedBy,
        ApprovedDate = t.ApprovedDate,
        AttachmentIds = t.AttachmentIds,
        RelatedUserId = t.RelatedUserId,
        CarId = t.CarId
    };

    public static UserDto ToDto(this User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        DisplayName = u.DisplayName,
        Role = u.Role,
        AssignedBranches = u.AssignedBranches,
        IsActive = u.IsActive,
        CreatedDate = u.CreatedDate
    };

    public static BranchDto ToDto(this Branch b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Code = b.Code,
        Address = b.Address,
        IsActive = b.IsActive
    };

    public static PettyCashRequestDto ToDto(this PettyCashRequest p) => new()
    {
        Id = p.Id,
        Amount = p.Amount,
        Reason = p.Reason,
        Branch = p.Branch,
        RequestedBy = p.RequestedBy,
        RequestedByName = p.RequestedByName,
        RequestedDate = p.RequestedDate,
        Status = p.Status,
        ApprovedBy = p.ApprovedBy,
        ApprovedDate = p.ApprovedDate,
        LinkedTransactionId = p.LinkedTransactionId
    };

    public static CarDto ToDto(this Car c) => new()
    {
        Id = c.Id,
        Branch = c.Branch,
        Client = c.Client,
        Type = c.Type,
        Model = c.Model,
        PlateNumber = c.PlateNumber,
        MonthlyBill = c.MonthlyBill,
        InitialDebt = c.InitialDebt,
        RemainingDebt = c.InitialDebt,
        ContractStartDate = c.ContractStartDate,
        ContractDurationMonths = c.ContractDurationMonths,
        ContractEndDate = c.ContractStartDate.AddMonths(c.ContractDurationMonths),
        Notes = c.Notes,
        IsActive = c.IsActive,
        CreatedBy = c.CreatedBy,
        CreatedDate = c.CreatedDate
    };

    public static InvoiceDto ToDto(this Invoice i) => new()
    {
        Id = i.Id,
        Type = i.Type,
        Branch = i.Branch,
        ClientName = i.ClientName,
        InvoiceDate = i.InvoiceDate,
        CarId = i.CarId,
        MonthlyBill = i.MonthlyBill,
        DriverName = i.DriverName,
        WageDeposit = i.WageDeposit,
        Fee = i.Fee,
        TaxScheme = i.TaxScheme,
        PpnAmount = i.PpnAmount,
        Pph23Amount = i.Pph23Amount,
        TotalAmount = i.TotalAmount,
        Status = i.Status,
        PaidDate = i.PaidDate,
        LinkedIncomeTransactionId = i.LinkedIncomeTransactionId,
        LinkedExpenseTransactionId = i.LinkedExpenseTransactionId,
        CreatedBy = i.CreatedBy,
        CreatedByName = i.CreatedByName,
        CreatedDate = i.CreatedDate
    };

    public static AuditLogDto ToDto(this AuditLog a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        UserName = a.UserName,
        Action = a.Action,
        Entity = a.Entity,
        EntityId = a.EntityId,
        OldValue = a.OldValue,
        NewValue = a.NewValue,
        Timestamp = a.Timestamp
    };
}
