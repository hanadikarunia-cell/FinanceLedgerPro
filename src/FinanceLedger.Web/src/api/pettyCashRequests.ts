import { axiosClient } from './axiosClient';
import type { PagedResult, PettyCashRequest, PettyCashRequestFilters, PettyCashRequestInput } from '@/types';

export const pettyCashRequestsApi = {
  async list(filters: PettyCashRequestFilters = {}): Promise<PagedResult<PettyCashRequest>> {
    const { data } = await axiosClient.get<PagedResult<PettyCashRequest>>('/petty-cash-requests', {
      params: filters,
    });
    return data;
  },

  async create(payload: PettyCashRequestInput): Promise<PettyCashRequest> {
    const { data } = await axiosClient.post<PettyCashRequest>('/petty-cash-requests', payload);
    return data;
  },

  async approve(id: string): Promise<PettyCashRequest> {
    const { data } = await axiosClient.post<PettyCashRequest>(`/petty-cash-requests/${id}/approve`);
    return data;
  },

  async reject(id: string, reason?: string): Promise<PettyCashRequest> {
    const { data } = await axiosClient.post<PettyCashRequest>(`/petty-cash-requests/${id}/reject`, { reason });
    return data;
  },

  async remove(id: string): Promise<void> {
    await axiosClient.delete(`/petty-cash-requests/${id}`);
  },
};
