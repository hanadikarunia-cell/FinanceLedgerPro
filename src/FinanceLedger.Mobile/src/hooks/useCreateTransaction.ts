import { useMutation, useQueryClient } from '@tanstack/react-query';
import { transactionsApi } from '@/api';
import { outbox } from '@/offline/outbox';
import { isOnline } from '@/offline/syncService';
import type { CreateTransactionRequest, Transaction } from '@/types';

export interface CreateTransactionResult {
  transaction: Transaction;
  queuedOffline: boolean;
}

/**
 * Creates a transaction. If the device is offline (or the POST fails), the
 * payload is durably enqueued in the outbox and an optimistic record is
 * returned. The sync service flushes the outbox when connectivity returns.
 */
export function useCreateTransaction() {
  const queryClient = useQueryClient();

  return useMutation<CreateTransactionResult, Error, CreateTransactionRequest>({
    mutationFn: async (payload) => {
      const online = await isOnline();
      if (!online) {
        const transaction = await outbox.enqueue(payload);
        return { transaction, queuedOffline: true };
      }
      try {
        const transaction = await transactionsApi.create(payload);
        return { transaction, queuedOffline: false };
      } catch (err) {
        // Network came online but the request still failed — queue it so we
        // never lose the user's entry.
        const transaction = await outbox.enqueue(payload);
        return { transaction, queuedOffline: true };
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      void queryClient.invalidateQueries({ queryKey: ['report'] });
    },
  });
}
