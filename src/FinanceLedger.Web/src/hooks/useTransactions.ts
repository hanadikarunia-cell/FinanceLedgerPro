import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';
import { transactionsApi } from '@/api/transactions';
import type { TransactionFilters, TransactionInput } from '@/types';

const KEY = 'transactions';

// Every mutation below invalidates the same set of query keys: the Dashboard, Reports, and
// Petty Cash Balance are all derived from the same transaction records as the Transactions
// list, so they must never go stale relative to each other after a create/edit/approve/
// reject/delete — otherwise the Dashboard and Transactions tab can show different numbers
// until the user manually refreshes.
function invalidateTransactionDerivedQueries(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: [KEY] });
  qc.invalidateQueries({ queryKey: ['dashboard'] });
  qc.invalidateQueries({ queryKey: ['pettyCashBalance'] });
  qc.invalidateQueries({ queryKey: ['reports'] });
}

export function useTransactions(filters: TransactionFilters) {
  return useQuery({
    queryKey: [KEY, filters],
    queryFn: () => transactionsApi.list(filters),
    placeholderData: keepPreviousData,
  });
}

export function useUsersLookup() {
  return useQuery({
    queryKey: ['usersLookup'],
    queryFn: () => transactionsApi.usersLookup(),
  });
}

export function useCreateTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: TransactionInput) => transactionsApi.create(payload),
    onSuccess: () => invalidateTransactionDerivedQueries(qc),
  });
}

export function useUpdateTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: TransactionInput }) =>
      transactionsApi.update(id, payload),
    onSuccess: () => invalidateTransactionDerivedQueries(qc),
  });
}

export function useDeleteTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => transactionsApi.remove(id),
    onSuccess: () => invalidateTransactionDerivedQueries(qc),
  });
}

export function useApproveTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => transactionsApi.approve(id),
    onSuccess: () => invalidateTransactionDerivedQueries(qc),
  });
}

export function useRejectTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      transactionsApi.reject(id, reason),
    onSuccess: () => invalidateTransactionDerivedQueries(qc),
  });
}
