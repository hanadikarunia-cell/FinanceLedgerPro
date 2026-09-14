import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { pettyCashRequestsApi } from '@/api/pettyCashRequests';
import type { PettyCashRequestFilters, PettyCashRequestInput } from '@/types';

const KEY = 'pettyCashRequests';

export function usePettyCashRequests(filters: PettyCashRequestFilters = {}) {
  return useQuery({
    queryKey: [KEY, filters],
    queryFn: () => pettyCashRequestsApi.list(filters),
  });
}

export function useCreatePettyCashRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: PettyCashRequestInput) => pettyCashRequestsApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useApprovePettyCashRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => pettyCashRequestsApi.approve(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [KEY] });
      qc.invalidateQueries({ queryKey: ['transactions'] });
      qc.invalidateQueries({ queryKey: ['dashboard'] });
      qc.invalidateQueries({ queryKey: ['pettyCashBalance'] });
      qc.invalidateQueries({ queryKey: ['reports'] });
    },
  });
}

export function useRejectPettyCashRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      pettyCashRequestsApi.reject(id, reason),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeletePettyCashRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => pettyCashRequestsApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}
