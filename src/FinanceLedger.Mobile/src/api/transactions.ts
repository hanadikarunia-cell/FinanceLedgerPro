import { apiClient } from './client';
import type {
  CreateTransactionRequest,
  Transaction,
  TransactionType,
} from '@/types';

export interface TransactionQuery {
  type?: TransactionType;
  from?: string;
  to?: string;
}

export const transactionsApi = {
  async list(query: TransactionQuery = {}): Promise<Transaction[]> {
    const { data } = await apiClient.get<Transaction[]>('/transactions', {
      params: query,
    });
    return data;
  },

  async create(body: CreateTransactionRequest): Promise<Transaction> {
    const { data } = await apiClient.post<Transaction>('/transactions', body);
    return data;
  },
};
