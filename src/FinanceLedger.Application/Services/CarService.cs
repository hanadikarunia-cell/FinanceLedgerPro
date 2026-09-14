using FinanceLedger.Application.Common;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Entities;
using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.Services;

public class CarService : ICarService
{
    private readonly ICarRepository _repo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public CarService(
        ICarRepository repo,
        ITransactionRepository transactionRepo,
        IInvoiceRepository invoiceRepo,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _repo = repo;
        _transactionRepo = transactionRepo;
        _invoiceRepo = invoiceRepo;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CarDto>> GetAllAsync(CancellationToken ct = default)
    {
        var cars = await _repo.GetAllAsync(ct);
        if (!_currentUser.IsManager)
            cars = cars.Where(c => _currentUser.AssignedBranches.Contains(c.Branch)).ToList();

        var dtos = new List<CarDto>();
        foreach (var car in cars)
            dtos.Add(await ToDtoWithDebtAsync(car, ct));
        return dtos;
    }

    public async Task<CarDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var car = await _repo.GetByIdAsync(id, ct);
        if (car is null) return null;

        if (!_currentUser.IsManager && !_currentUser.AssignedBranches.Contains(car.Branch))
            throw new ForbiddenException("You do not have access to this car.");

        return await ToDtoWithDebtAsync(car, ct);
    }

    public async Task<CarDto> CreateAsync(CreateCarDto dto, CancellationToken ct = default)
    {
        if (!_currentUser.IsManager && !_currentUser.AssignedBranches.Contains(dto.Branch))
            throw new ForbiddenException("You can only add cars to your assigned branches.");

        var car = new Car
        {
            Branch = dto.Branch,
            Client = dto.Client,
            Type = dto.Type,
            Model = dto.Model,
            PlateNumber = dto.PlateNumber,
            MonthlyBill = dto.MonthlyBill,
            InitialDebt = dto.InitialDebt,
            ContractStartDate = dto.ContractStartDate,
            ContractDurationMonths = dto.ContractDurationMonths,
            Notes = dto.Notes,
            CreatedBy = _currentUser.UserId ?? "unknown",
            CreatedDate = DateTime.UtcNow
        };

        var created = await _repo.AddAsync(car, ct);
        await _audit.LogAsync(AuditAction.Create, nameof(Car), created.Id, null, created.ToDto(), ct);
        return await ToDtoWithDebtAsync(created, ct);
    }

    public async Task<CarDto> UpdateAsync(string id, UpdateCarDto dto, CancellationToken ct = default)
    {
        var car = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Car), id);

        if (!_currentUser.IsManager
            && (!_currentUser.AssignedBranches.Contains(car.Branch) || !_currentUser.AssignedBranches.Contains(dto.Branch)))
            throw new ForbiddenException("You can only edit cars in your assigned branches.");

        var old = car.ToDto();
        car.Branch = dto.Branch;
        car.Client = dto.Client;
        car.Type = dto.Type;
        car.Model = dto.Model;
        car.PlateNumber = dto.PlateNumber;
        car.MonthlyBill = dto.MonthlyBill;
        car.InitialDebt = dto.InitialDebt;
        car.ContractStartDate = dto.ContractStartDate;
        car.ContractDurationMonths = dto.ContractDurationMonths;
        car.Notes = dto.Notes;
        car.IsActive = dto.IsActive;

        var updated = await _repo.UpdateAsync(car, ct);
        await _audit.LogAsync(AuditAction.Update, nameof(Car), updated.Id, old, updated.ToDto(), ct);
        return await ToDtoWithDebtAsync(updated, ct);
    }

    private async Task<CarDto> ToDtoWithDebtAsync(Car car, CancellationToken ct)
    {
        var paid = (await _transactionRepo.FindAsync(
            t => t.CarId == car.Id
                && t.Type == TransactionType.Expense
                && t.Category == "Car Debt"
                && t.ApprovalStatus == ApprovalStatus.Approved,
            ct)).Sum(t => t.Amount);

        // Each Paid Rental invoice for this car represents one billing period settled —
        // the term counts down as invoices get paid, the same way debt counts down as
        // Car Debt payments are approved.
        var paidBillingPeriods = (await _invoiceRepo.FindAsync(
            i => i.CarId == car.Id
                && i.Type == InvoiceType.Rental
                && i.Status == InvoiceStatus.Paid,
            ct)).Count;

        var dto = car.ToDto();
        dto.RemainingDebt = car.InitialDebt - paid;
        dto.PaidBillingPeriods = paidBillingPeriods;
        dto.RemainingBillingPeriods = Math.Max(0, car.ContractDurationMonths - paidBillingPeriods);
        return dto;
    }
}
