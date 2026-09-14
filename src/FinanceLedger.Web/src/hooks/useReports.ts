import { useQuery } from '@tanstack/react-query';
import { reportsApi } from '@/api/reports';
import type { ReportFilters, ReportPeriod } from '@/types';

export function useReports(period: ReportPeriod, filters: ReportFilters, enabled = true) {
  return useQuery({
    queryKey: ['reports', period, filters],
    queryFn: () => reportsApi.get(period, filters),
    enabled,
  });
}
