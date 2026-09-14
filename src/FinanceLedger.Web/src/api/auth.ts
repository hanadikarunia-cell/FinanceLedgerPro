import { axiosClient } from './axiosClient';
import type { AuthResponse, LoginRequest } from '@/types';

export const authApi = {
  async login(payload: LoginRequest): Promise<AuthResponse> {
    const { data } = await axiosClient.post<AuthResponse>('/auth/login', payload);
    return data;
  },

  async refresh(refreshToken: string): Promise<AuthResponse> {
    const { data } = await axiosClient.post<AuthResponse>('/auth/refresh', { refreshToken });
    return data;
  },
};
