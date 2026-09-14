import { axiosClient } from './axiosClient';
import type { ReportFilters, ReportPeriod, ReportResult } from '@/types';

export const reportsApi = {
  async get(period: ReportPeriod, filters: ReportFilters = {}): Promise<ReportResult> {
    const { data } = await axiosClient.get<ReportResult>(`/reports/${period}`, {
      params: filters,
    });
    return data;
  },
};
