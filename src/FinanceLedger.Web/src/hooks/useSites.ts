import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { sitesApi } from '@/api/sites';
import type { CreateSiteInput, UpdateSiteInput } from '@/types';

const KEY = 'sites';

export function useSites(enabled = true) {
  return useQuery({ queryKey: [KEY], queryFn: () => sitesApi.list(), enabled });
}

export function useSiteUsers(siteId: string | null) {
  return useQuery({
    queryKey: [KEY, siteId, 'users'],
    queryFn: () => sitesApi.users(siteId as string),
    enabled: !!siteId,
  });
}

export function useCreateSite() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateSiteInput) => sitesApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateSite() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateSiteInput }) =>
      sitesApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}
