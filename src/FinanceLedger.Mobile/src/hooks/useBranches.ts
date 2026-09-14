import { useQuery } from '@tanstack/react-query';
import { branchesApi } from '@/api';
import { cache, cacheKeys } from '@/offline/cache';
import type { Branch } from '@/types';

/**
 * Branches list with offline fallback: tries the network, writes-through to
 * cache on success, and falls back to the cached copy on failure.
 */
export function useBranches() {
  return useQuery<Branch[]>({
    queryKey: ['branches'],
    queryFn: async () => {
      try {
        const data = await branchesApi.list();
        await cache.set(cacheKeys.branches, data);
        return data;
      } catch (err) {
        const cached = await cache.get<Branch[]>(cacheKeys.branches);
        if (cached) return cached;
        throw err;
      }
    },
    staleTime: 1000 * 60 * 30,
  });
}
