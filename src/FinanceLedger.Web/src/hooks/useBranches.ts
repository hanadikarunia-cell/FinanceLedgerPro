import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { branchesApi } from '@/api/branches';
import type { BranchInput } from '@/types';

const KEY = 'branches';

export function useBranches() {
  return useQuery({ queryKey: [KEY], queryFn: () => branchesApi.list() });
}

export function useCreateBranch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: BranchInput) => branchesApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}
