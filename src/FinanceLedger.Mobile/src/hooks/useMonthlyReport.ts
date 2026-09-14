import { useQuery } from '@tanstack/react-query';
import { reportsApi } from '@/api';
import { cache, cacheKeys } from '@/offline/cache';
import type { MonthlyReport } from '@/types';

export function useMonthlyReport(year: number, month: number) {
  return useQuery<MonthlyReport>({
    queryKey: ['report', 'monthly', year, month],
    queryFn: async () => {
      try {
        const data = await reportsApi.monthly({ year, month });
        await cache.set(cacheKeys.monthlyReport(year, month), data);
        return data;
      } catch (err) {
        const cached = await cache.get<MonthlyReport>(
          cacheKeys.monthlyReport(year, month),
        );
        if (cached) return cached;
        throw err;
      }
    },
    staleTime: 1000 * 60 * 10,
  });
}
