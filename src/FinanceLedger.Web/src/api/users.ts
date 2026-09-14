import { axiosClient } from './axiosClient';
import type { User, UserInput } from '@/types';

export const usersApi = {
  async list(): Promise<User[]> {
    const { data } = await axiosClient.get<User[]>('/users');
    return data;
  },

  async create(payload: UserInput): Promise<User> {
    const { data } = await axiosClient.post<User>('/users', payload);
    return data;
  },

  async update(id: string, payload: UserInput): Promise<User> {
    const { data } = await axiosClient.put<User>(`/users/${id}`, payload);
    return data;
  },
};
