import { apiClient } from './client';
import type { Branch } from '@/types';

export const branchesApi = {
  async list(): Promise<Branch[]> {
    const { data } = await apiClient.get<Branch[]>('/branches');
    return data;
  },
};
