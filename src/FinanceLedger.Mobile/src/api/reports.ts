import { apiClient } from './client';
import type { MonthlyReport } from '@/types';

export interface MonthlyReportQuery {
  month?: number; // 1-12
  year?: number;
}

export const reportsApi = {
  async monthly(query: MonthlyReportQuery = {}): Promise<MonthlyReport> {
    const { data } = await apiClient.get<MonthlyReport>('/reports/monthly', {
      params: query,
    });
    return data;
  },
};
