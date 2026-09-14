export type UserRole = 'Manager' | 'User';

export type TransactionType = 'Income' | 'Expense';

export type ApprovalStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected';

export interface User {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  assignedBranches: string[];
  isActive?: boolean;
}

export interface UserLookup {
  id: string;
  displayName: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  tokenType: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface Branch {
  id: string;
  name: string;
  code: string;
  address?: string;
  isActive?: boolean;
}

export interface Car {
  id: string;
  branch: string; // branchId
  client: string;
  type: string;
  model: string;
  plateNumber: string;
  monthlyBill: number;
  initialDebt: number;
  remainingDebt: number;
  contractStartDate: string;
  contractDurationMonths: number;
  contractEndDate: string;
  paidBillingPeriods: number;
  remainingBillingPeriods: number;
  notes?: string;
  isActive: boolean;
  createdBy: string;
  createdDate: string;
}

export interface CarLookup {
  id: string;
  label: string;
}

export interface Attachment {
  id: string;
  fileName: string;
  url: string;
  contentType?: string;
  size?: number;
}

export interface Transaction {
  id: string;
  type: TransactionType;
  category: string;
  description?: string;
  amount: number;
  date: string; // ISO date
  branch: string; // branchId
  createdBy: string; // userId
  createdByName: string;
  createdDate: string;
  approvalStatus: ApprovalStatus;
  approvedBy?: string;
  approvedDate?: string;
  attachmentIds: string[];
  relatedUserId?: string;
  carId?: string;
}

export interface TransactionInput {
  type: TransactionType;
  category: string;
  description?: string;
  amount: number;
  date: string;
  branch: string; // branchId
  attachmentIds?: string[];
  relatedUserId?: string;
  carId?: string;
}

export interface TransactionFilters {
  type?: TransactionType;
  category?: string;
  branch?: string;
  userId?: string;
  from?: string;
  to?: string;
  status?: ApprovalStatus;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface MonthlySeriesPoint {
  year: number;
  month: number;
  label: string;
  income: number;
  expense: number;
  net: number;
}

export interface CategorySlice {
  category: string;
  amount: number;
  count: number;
}

export interface DashboardSummary {
  totalIncome: number;
  totalExpense: number;
  netBalance: number;
  totalPettyCashIssued: number;
  totalPettyCashExpenses: number;
  totalPettyCashOutstanding: number;
  transactionCount: number;
  pendingApprovals: number;
  monthlySeries: MonthlySeriesPoint[];
  topCategories: CategorySlice[];
  recent: Transaction[];
}

export type DashboardMetric =
  | 'income'
  | 'expense'
  | 'balance'
  | 'pettyCashIssued'
  | 'pettyCashExpenses'
  | 'pettyCashOutstanding';

export interface PettyCashUserSummary {
  userId: string;
  displayName: string;
  issued: number;
  spent: number;
  outstanding: number;
}

export interface DashboardBreakdown {
  transactions: Transaction[];
  userSummaries: PettyCashUserSummary[];
}

export interface ReportFilters {
  from?: string;
  to?: string;
  category?: string;
  userId?: string;
  branch?: string;
}

export type ReportPeriod = 'daily' | 'monthly' | 'yearly';

export interface ReportResult {
  period: string;
  from: string;
  to: string;
  totalIncome: number;
  totalExpense: number;
  netBalance: number;
  totalPettyCashIssued: number;
  transactionCount: number;
  incomeByCategory: CategorySlice[];
  expenseByCategory: CategorySlice[];
  transactions: Transaction[];
}

export type ExportFormat = 'excel' | 'pdf' | 'csv';

export interface UserInput {
  email: string;
  displayName: string;
  role: UserRole;
  assignedBranches: string[];
  password?: string;
  isActive?: boolean;
}

export interface BranchInput {
  name: string;
  code: string;
  address?: string;
  isActive?: boolean;
}

export interface CarInput {
  branch: string;
  client: string;
  type: string;
  model: string;
  plateNumber: string;
  monthlyBill: number;
  initialDebt: number;
  contractStartDate: string;
  contractDurationMonths: number;
  notes?: string;
  isActive?: boolean;
}

export interface PettyCashRequest {
  id: string;
  amount: number;
  reason: string;
  branch: string;
  requestedBy: string;
  requestedByName: string;
  requestedDate: string;
  status: ApprovalStatus;
  approvedBy?: string;
  approvedDate?: string;
  linkedTransactionId?: string;
}

export interface PettyCashRequestInput {
  amount: number;
  reason: string;
  branch: string;
}

export interface PettyCashRequestFilters {
  branch?: string;
  status?: ApprovalStatus;
  page?: number;
  pageSize?: number;
}

export type InvoiceType = 'Rental' | 'ServiceBill';
export type InvoiceStatus = 'Unpaid' | 'Paid';
export type TaxScheme = 'Combined' | 'WageOnly' | 'FeeOnly';

export interface Invoice {
  id: string;
  type: InvoiceType;
  branch: string;
  clientName: string;
  invoiceDate: string;
  carId?: string;
  monthlyBill?: number;
  driverName?: string;
  wageDeposit?: number;
  fee?: number;
  taxScheme?: TaxScheme;
  ppnAmount: number;
  pph23Amount: number;
  totalAmount: number;
  status: InvoiceStatus;
  paidDate?: string;
  linkedIncomeTransactionId?: string;
  linkedExpenseTransactionId?: string;
  createdBy: string;
  createdByName: string;
  createdDate: string;
}

export interface CreateInvoiceInput {
  type: InvoiceType;
  branch: string;
  clientName?: string;
  invoiceDate: string;
  carId?: string;
  driverName?: string;
  wageDeposit?: number;
  fee?: number;
  taxScheme?: TaxScheme;
}

export interface InvoiceFilters {
  branch?: string;
  type?: InvoiceType;
  status?: InvoiceStatus;
  page?: number;
  pageSize?: number;
}

export interface AuditLog {
  id: string;
  timestamp: string;
  userId: string;
  userName?: string;
  action: string;
  entity: string;
  entityId?: string;
  details?: string;
  ipAddress?: string;
}

export interface AuditLogFilters {
  userId?: string;
  action?: string;
  entity?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
