import { axiosClient } from './axiosClient';
import type { AuthResponse, LoginRequest, MeResponse } from '@/types';

export const authApi = {
  async login(payload: LoginRequest): Promise<AuthResponse> {
    const { data } = await axiosClient.post<AuthResponse>('/auth/login', payload);
    return data;
  },

  /** The effective identity for this request (the acted-as user, if any). */
  async me(): Promise<MeResponse> {
    const { data } = await axiosClient.get<MeResponse>('/auth/me');
    return data;
  },

  async refresh(refreshToken: string): Promise<AuthResponse> {
    const { data } = await axiosClient.post<AuthResponse>('/auth/refresh', { refreshToken });
    return data;
  },
};
