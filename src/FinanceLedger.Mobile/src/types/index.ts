export type TransactionType = 'Income' | 'Expense';

export type ApprovalStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected';

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse extends AuthTokens {
  user: UserProfile;
}

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  role?: string;
}

export interface Branch {
  id: string;
  name: string;
  code?: string;
}

export interface Transaction {
  id: string;
  type: TransactionType;
  category: string;
  description: string;
  amount: number;
  /** ISO 8601 date string (yyyy-MM-dd). */
  date: string;
  branch: string;
  approvalStatus: ApprovalStatus;
  createdAt?: string;
}

/** Payload sent to POST /transactions. No server id yet. */
export interface CreateTransactionRequest {
  type: TransactionType;
  category: string;
  description: string;
  amount: number;
  date: string;
  branch: string;
  approvalStatus: ApprovalStatus;
}

export interface DashboardSummary {
  totalIncome: number;
  totalExpense: number;
  netBalance: number;
  pendingApprovals: number;
  /** last 6 months of net flow for the chart */
  trend: MonthlyPoint[];
}

export interface MonthlyPoint {
  /** e.g. "Jan", "Feb" */
  label: string;
  income: number;
  expense: number;
}

export interface MonthlyReport {
  month: string;
  year: number;
  totalIncome: number;
  totalExpense: number;
  net: number;
  lines: MonthlyReportLine[];
}

export interface MonthlyReportLine {
  category: string;
  type: TransactionType;
  amount: number;
}

/** An item queued locally while offline, awaiting flush to the server. */
export interface OutboxItem {
  /** local-only client id */
  localId: string;
  payload: CreateTransactionRequest;
  createdAt: string;
  attempts: number;
  lastError?: string;
}
