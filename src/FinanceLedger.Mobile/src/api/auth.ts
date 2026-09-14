import { apiClient } from './client';
import type { LoginRequest, LoginResponse } from '@/types';

export const authApi = {
  async login(body: LoginRequest): Promise<LoginResponse> {
    const { data } = await apiClient.post<LoginResponse>('/auth/login', body);
    return data;
  },
};
