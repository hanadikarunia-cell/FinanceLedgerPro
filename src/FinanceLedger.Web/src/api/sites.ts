import { axiosClient } from './axiosClient';
import type { CreateSiteInput, Site, UpdateSiteInput, User } from '@/types';

export const sitesApi = {
  async list(): Promise<Site[]> {
    const { data } = await axiosClient.get<Site[]>('/sites');
    return data;
  },

  async create(payload: CreateSiteInput): Promise<Site> {
    const { data } = await axiosClient.post<Site>('/sites', payload);
    return data;
  },

  async update(id: string, payload: UpdateSiteInput): Promise<Site> {
    const { data } = await axiosClient.put<Site>(`/sites/${id}`, payload);
    return data;
  },

  async users(id: string): Promise<User[]> {
    const { data } = await axiosClient.get<User[]>(`/sites/${id}/users`);
    return data;
  },
};
