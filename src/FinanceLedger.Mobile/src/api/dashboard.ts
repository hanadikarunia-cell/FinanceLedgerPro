import { apiClient } from './client';
import type { DashboardSummary } from '@/types';

export const dashboardApi = {
  async get(): Promise<DashboardSummary> {
    const { data } = await apiClient.get<DashboardSummary>('/dashboard');
    return data;
  },
};
