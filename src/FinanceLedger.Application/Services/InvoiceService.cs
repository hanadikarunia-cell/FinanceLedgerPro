using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class InvoiceService : IInvoiceService
{
    private const decimal PpnRate = 0.11m;
    private const decimal Pph23Rate = 0.02m;

    private readonly IInvoiceRepository _repo;
    private readonly ICarRepository _carRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public InvoiceService(
        IInvoiceRepository repo,
        ICarRepository carRepo,
        ITransactionRepository transactionRepo,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _repo = repo;
        _carRepo = carRepo;
        _transactionRepo = transactionRepo;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResult<InvoiceDto>> QueryAsync(InvoiceQuery query, CancellationToken ct = default)
    {
        IReadOnlyCollection<string>? restrictBranches = _currentUser.IsManager ? null : _currentUser.AssignedBranches;
        var page = await _repo.QueryAsync(query, restrictBranches, ct);
        return new PagedResult<InvoiceDto>(
            page.Items.Select(i => i.ToDto()).ToList(),
            page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can create invoices.");

        var invoice = dto.Type == InvoiceType.Rental
            ? await BuildRentalInvoiceAsync(dto, ct)
            : BuildServiceBillInvoice(dto);

        invoice.CreatedBy = _currentUser.UserId ?? "unknown";
        invoice.CreatedByName = _currentUser.UserName ?? "unknown";
        invoice.CreatedDate = DateTime.UtcNow;
        invoice.Status = InvoiceStatus.Unpaid;

        var created = await _repo.AddAsync(invoice, ct);
        await _audit.LogAsync(AuditAction.Create, nameof(Invoice), created.Id, null, created.ToDto(), ct);
        return created.ToDto();
    }

    private async Task<Invoice> BuildRentalInvoiceAsync(CreateInvoiceDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.CarId))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["carId"] = new[] { "Select which car this rental invoice is for." }
            });

        var car = await _carRepo.GetByIdAsync(dto.CarId, ct)
            ?? throw new NotFoundException(nameof(Car), dto.CarId);

        var ppn = Math.Round(car.MonthlyBill * PpnRate, 0);
        var total = car.MonthlyBill + ppn;

        return new Invoice
        {
            Type = InvoiceType.Rental,
            Branch = dto.Branch,
            ClientName = car.Client,
            InvoiceDate = dto.InvoiceDate,
            CarId = car.Id,
            MonthlyBill = car.MonthlyBill,
            PpnAmount = ppn,
            Pph23Amount = 0,
            TotalAmount = total
        };
    }

    private Invoice BuildServiceBillInvoice(CreateInvoiceDto dto)
    {
        if (dto.WageDeposit is null || dto.Fee is null || dto.TaxScheme is null)
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["taxScheme"] = new[] { "Wage Deposit, Fee, and a tax scheme are required for a Service Bill invoice." }
            });

        var wage = dto.WageDeposit.Value;
        var fee = dto.Fee.Value;

        var (ppnBase, pph23Base) = dto.TaxScheme.Value switch
        {
            TaxScheme.Combined => (wage + fee, fee),
            TaxScheme.WageOnly => (wage, wage),
            TaxScheme.FeeOnly => (fee, fee),
            _ => (wage + fee, fee)
        };

        var ppn = Math.Round(ppnBase * PpnRate, 0);
        var pph23 = Math.Round(pph23Base * Pph23Rate, 0);
        var total = wage + fee + ppn - pph23;

        return new Invoice
        {
            Type = InvoiceType.ServiceBill,
            Branch = dto.Branch,
            ClientName = dto.ClientName,
            InvoiceDate = dto.InvoiceDate,
            DriverName = dto.DriverName,
            WageDeposit = wage,
            Fee = fee,
            TaxScheme = dto.TaxScheme,
            PpnAmount = ppn,
            Pph23Amount = pph23,
            TotalAmount = total
        };
    }

    public async Task<InvoiceDto> MarkPaidAsync(string id, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can mark invoices as paid.");

        var invoice = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Invoice), id);

        if (invoice.Status == InvoiceStatus.Paid)
            throw new ConflictException("This invoice has already been marked as paid.");

        var old = invoice.ToDto();

        // Income side: Wage Deposit + Fee (or Monthly Bill) + PPN — all recognized as revenue
        // once the client actually pays.
        var incomeAmount = invoice.Type == InvoiceType.Rental
            ? (invoice.MonthlyBill ?? 0) + invoice.PpnAmount
            : (invoice.WageDeposit ?? 0) + (invoice.Fee ?? 0) + invoice.PpnAmount;

        var incomeTx = new Transaction
        {
            Type = TransactionType.Income,
            Category = "Invoice",
            Description = invoice.Type == InvoiceType.Rental
                ? $"Rental invoice paid — {invoice.ClientName}"
                : $"Service bill paid — {invoice.ClientName}" + (invoice.DriverName is { Length: > 0 } d ? $" (driver: {d})" : ""),
            Amount = incomeAmount,
            Date = DateTime.UtcNow,
            Branch = invoice.Branch,
            CreatedBy = _currentUser.UserId ?? "unknown",
            CreatedByName = _currentUser.UserName ?? "unknown",
            CreatedDate = DateTime.UtcNow,
            ApprovalStatus = ApprovalStatus.Approved,
            CarId = invoice.CarId
        };
        var createdIncomeTx = await _transactionRepo.AddAsync(incomeTx, ct);
        await _audit.LogAsync(AuditAction.Create, nameof(Transaction), createdIncomeTx.Id, null, createdIncomeTx, ct);

        string? expenseTxId = null;
        if (invoice.Pph23Amount > 0)
        {
            var expenseTx = new Transaction
            {
                Type = TransactionType.Expense,
                Category = "Taxes - PPH23",
                Description = $"PPH23 withheld — {invoice.ClientName}",
                Amount = invoice.Pph23Amount,
                Date = DateTime.UtcNow,
                Branch = invoice.Branch,
                CreatedBy = _currentUser.UserId ?? "unknown",
                CreatedByName = _currentUser.UserName ?? "unknown",
                CreatedDate = DateTime.UtcNow,
                ApprovalStatus = ApprovalStatus.Approved,
                CarId = invoice.CarId
            };
            var createdExpenseTx = await _transactionRepo.AddAsync(expenseTx, ct);
            await _audit.LogAsync(AuditAction.Create, nameof(Transaction), createdExpenseTx.Id, null, createdExpenseTx, ct);
            expenseTxId = createdExpenseTx.Id;
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidDate = DateTime.UtcNow;
        invoice.LinkedIncomeTransactionId = createdIncomeTx.Id;
        invoice.LinkedExpenseTransactionId = expenseTxId;

        var updated = await _repo.UpdateAsync(invoice, ct);
        await _audit.LogAsync(AuditAction.Update, nameof(Invoice), updated.Id, old, updated.ToDto(), ct);
        return updated.ToDto();
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can delete invoices.");

        var invoice = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Invoice), id);

        // A Paid invoice already has real ledger transactions linked to it — deleting the
        // invoice record without touching those would leave an untraceable income/expense
        // entry. Void it first (which removes those transactions), then delete.
        if (invoice.Status == InvoiceStatus.Paid)
            throw new ForbiddenException("Paid invoices cannot be deleted directly; void it first.");

        await _repo.DeleteAsync(invoice, ct);
        await _audit.LogAsync(AuditAction.Delete, nameof(Invoice), id, invoice.ToDto(), null, ct);
    }

    public async Task<InvoiceDto> VoidAsync(string id, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager)
            throw new ForbiddenException("Only Managers can void invoices.");

        var invoice = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Invoice), id);

        if (invoice.Status != InvoiceStatus.Paid)
            throw new ConflictException("Only paid invoices can be voided.");

        var old = invoice.ToDto();

        // Reverse both ledger transactions this invoice created when it was marked paid,
        // then reopen it as Unpaid so it goes back into Accounts Receivable and no longer
        // counts toward the car's Remaining Debt / Remaining Billing Periods.
        if (invoice.LinkedIncomeTransactionId is not null)
        {
            var incomeTx = await _transactionRepo.GetByIdAsync(invoice.LinkedIncomeTransactionId, ct);
            if (incomeTx is not null)
            {
                await _transactionRepo.DeleteAsync(incomeTx, ct);
                await _audit.LogAsync(AuditAction.Delete, nameof(Transaction), incomeTx.Id, incomeTx, null, ct);
            }
        }

        if (invoice.LinkedExpenseTransactionId is not null)
        {
            var expenseTx = await _transactionRepo.GetByIdAsync(invoice.LinkedExpenseTransactionId, ct);
            if (expenseTx is not null)
            {
                await _transactionRepo.DeleteAsync(expenseTx, ct);
                await _audit.LogAsync(AuditAction.Delete, nameof(Transaction), expenseTx.Id, expenseTx, null, ct);
            }
        }

        invoice.Status = InvoiceStatus.Unpaid;
        invoice.PaidDate = null;
        invoice.LinkedIncomeTransactionId = null;
        invoice.LinkedExpenseTransactionId = null;

        var updated = await _repo.UpdateAsync(invoice, ct);
        await _audit.LogAsync(AuditAction.Void, nameof(Invoice), updated.Id, old, updated.ToDto(), ct);
        return updated.ToDto();
    }
}
