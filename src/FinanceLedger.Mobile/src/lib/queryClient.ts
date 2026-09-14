import { QueryClient } from '@tanstack/react-query';

/**
 * Shared react-query client. Retries are conservative because our hooks already
 * fall back to the offline cache on failure — no need to hammer a dead network.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 1000 * 60,
      gcTime: 1000 * 60 * 60 * 24,
    },
    mutations: {
      retry: 0,
    },
  },
});
