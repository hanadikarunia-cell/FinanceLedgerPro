import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from '@/api';
import { cache, cacheKeys } from '@/offline/cache';
import type { DashboardSummary } from '@/types';

/** Dashboard summary with write-through cache and offline fallback. */
export function useDashboard() {
  return useQuery<DashboardSummary>({
    queryKey: ['dashboard'],
    queryFn: async () => {
      try {
        const data = await dashboardApi.get();
        await cache.set(cacheKeys.dashboard, data);
        return data;
      } catch (err) {
        const cached = await cache.get<DashboardSummary>(cacheKeys.dashboard);
        if (cached) return cached;
        throw err;
      }
    },
    staleTime: 1000 * 60 * 5,
  });
}
