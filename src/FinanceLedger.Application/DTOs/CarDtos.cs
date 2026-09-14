namespace FinanceLedger.Application.DTOs;

public class CarDto
{
    public string Id { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public decimal MonthlyBill { get; set; }
    public decimal InitialDebt { get; set; }
    public decimal RemainingDebt { get; set; }
    public DateTime ContractStartDate { get; set; }
    public int ContractDurationMonths { get; set; }
    public DateTime ContractEndDate { get; set; }
    public int PaidBillingPeriods { get; set; }
    public int RemainingBillingPeriods { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class CreateCarDto
{
    public string Branch { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public decimal MonthlyBill { get; set; }
    public decimal InitialDebt { get; set; }
    public DateTime ContractStartDate { get; set; }
    public int ContractDurationMonths { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCarDto
{
    public string Branch { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public decimal MonthlyBill { get; set; }
    public decimal InitialDebt { get; set; }
    public DateTime ContractStartDate { get; set; }
    public int ContractDurationMonths { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
