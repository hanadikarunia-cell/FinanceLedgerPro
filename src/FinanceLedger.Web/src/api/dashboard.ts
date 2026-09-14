import { axiosClient } from './axiosClient';
import type { DashboardBreakdown, DashboardMetric, DashboardSummary } from '@/types';

export const dashboardApi = {
  async get(params?: { from?: string; to?: string; branch?: string }): Promise<DashboardSummary> {
    const { data } = await axiosClient.get<DashboardSummary>('/dashboard', { params });
    return data;
  },

  async breakdown(metric: DashboardMetric): Promise<DashboardBreakdown> {
    const { data } = await axiosClient.get<DashboardBreakdown>('/dashboard/breakdown', {
      params: { metric },
    });
    return data;
  },
};
