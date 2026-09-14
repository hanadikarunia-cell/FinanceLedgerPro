import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from '@/api/dashboard';
import type { DashboardMetric } from '@/types';

export function useDashboard(params?: { from?: string; to?: string; branch?: string }) {
  return useQuery({
    queryKey: ['dashboard', params],
    queryFn: () => dashboardApi.get(params),
  });
}

export function useDashboardBreakdown(metric: DashboardMetric | null) {
  return useQuery({
    queryKey: ['dashboard', 'breakdown', metric],
    queryFn: () => dashboardApi.breakdown(metric as DashboardMetric),
    enabled: metric !== null,
  });
}
