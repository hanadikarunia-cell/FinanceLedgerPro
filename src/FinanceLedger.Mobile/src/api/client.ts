import axios, {
  AxiosError,
  AxiosInstance,
  InternalAxiosRequestConfig,
} from 'axios';
import { API_ROOT } from '@/config/env';
import { tokenStorage } from '@/auth/tokenStorage';
import type { AuthTokens } from '@/types';

/**
 * Callback invoked when refresh fails irrecoverably (both tokens dead).
 * AuthContext registers this so it can force a logout / route to Login.
 */
type SessionExpiredHandler = () => void;
let onSessionExpired: SessionExpiredHandler | null = null;
export function setSessionExpiredHandler(fn: SessionExpiredHandler): void {
  onSessionExpired = fn;
}

export const apiClient: AxiosInstance = axios.create({
  baseURL: API_ROOT,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

// --- Request interceptor: attach the current access token -------------------
apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  const token = await tokenStorage.getAccessToken();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

// --- Response interceptor: transparent refresh on 401 ----------------------
// A single in-flight refresh is shared so concurrent 401s don't stampede.
let refreshPromise: Promise<string> | null = null;

async function performRefresh(): Promise<string> {
  const refreshToken = await tokenStorage.getRefreshToken();
  if (!refreshToken) {
    throw new Error('No refresh token available');
  }
  // Use a bare axios call so we don't recurse through this interceptor.
  const { data } = await axios.post<AuthTokens>(
    `${API_ROOT}/auth/refresh`,
    { refreshToken },
    { headers: { 'Content-Type': 'application/json' }, timeout: 15000 },
  );
  await tokenStorage.saveTokens(data);
  return data.accessToken;
}

interface RetriableConfig extends InternalAxiosRequestConfig {
  _retried?: boolean;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetriableConfig | undefined;

    // Only handle a real 401 once, and never for the refresh endpoint itself.
    const isAuthEndpoint = original?.url?.includes('/auth/');
    if (
      error.response?.status !== 401 ||
      !original ||
      original._retried ||
      isAuthEndpoint
    ) {
      return Promise.reject(error);
    }

    original._retried = true;

    try {
      if (!refreshPromise) {
        refreshPromise = performRefresh().finally(() => {
          refreshPromise = null;
        });
      }
      const newAccessToken = await refreshPromise;
      original.headers.set('Authorization', `Bearer ${newAccessToken}`);
      return apiClient(original);
    } catch (refreshErr) {
      await tokenStorage.clear();
      onSessionExpired?.();
      return Promise.reject(refreshErr);
    }
  },
);
