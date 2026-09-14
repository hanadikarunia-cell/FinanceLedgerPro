import { axiosClient } from './axiosClient';
import type { Branch, BranchInput } from '@/types';

export const branchesApi = {
  async list(): Promise<Branch[]> {
    const { data } = await axiosClient.get<Branch[]>('/branches');
    return data;
  },

  async create(payload: BranchInput): Promise<Branch> {
    const { data } = await axiosClient.post<Branch>('/branches', payload);
    return data;
  },
};
