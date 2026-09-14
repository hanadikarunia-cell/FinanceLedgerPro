import { axiosClient } from './axiosClient';
import type {
  PagedResult,
  Transaction,
  TransactionFilters,
  TransactionInput,
  UserLookup,
} from '@/types';

export const transactionsApi = {
  async pettyCashBalance(): Promise<number> {
    const { data } = await axiosClient.get<{ balance: number }>('/transactions/petty-cash-balance');
    return data.balance;
  },

  async usersLookup(): Promise<UserLookup[]> {
    const { data } = await axiosClient.get<UserLookup[]>('/transactions/users-lookup');
    return data;
  },

  async list(filters: TransactionFilters = {}): Promise<PagedResult<Transaction>> {
    const { data } = await axiosClient.get<PagedResult<Transaction>>('/transactions', {
      params: filters,
    });
    return data;
  },

  async get(id: string): Promise<Transaction> {
    const { data } = await axiosClient.get<Transaction>(`/transactions/${id}`);
    return data;
  },

  async create(payload: TransactionInput): Promise<Transaction> {
    const { data } = await axiosClient.post<Transaction>('/transactions', payload);
    return data;
  },

  async update(id: string, payload: TransactionInput): Promise<Transaction> {
    const { data } = await axiosClient.put<Transaction>(`/transactions/${id}`, payload);
    return data;
  },

  async remove(id: string): Promise<void> {
    await axiosClient.delete(`/transactions/${id}`);
  },

  async approve(id: string): Promise<Transaction> {
    const { data } = await axiosClient.post<Transaction>(`/transactions/${id}/approve`);
    return data;
  },

  async reject(id: string, reason?: string): Promise<Transaction> {
    const { data } = await axiosClient.post<Transaction>(`/transactions/${id}/reject`, {
      reason,
    });
    return data;
  },
};
