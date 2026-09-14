import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';
import { tokenStore } from './tokenStore';

const API_BASE = `${import.meta.env.VITE_API_BASE_URL}/api/v1`;

export const axiosClient: AxiosInstance = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
});

// ---- Request interceptor: attach bearer token -------------------------------
axiosClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = tokenStore.getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ---- Response interceptor: refresh on 401 -----------------------------------
interface RetriableConfig extends AxiosRequestConfig {
  _retry?: boolean;
}

let isRefreshing = false;
let pendingQueue: Array<(token: string | null) => void> = [];

function flushQueue(token: string | null) {
  pendingQueue.forEach((cb) => cb(token));
  pendingQueue = [];
}

function handleAuthFailure() {
  tokenStore.clear();
  if (window.location.pathname !== '/login') {
    window.location.assign('/login');
  }
}

axiosClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetriableConfig | undefined;

    if (
      error.response?.status !== 401 ||
      !original ||
      original._retry ||
      original.url?.includes('/auth/refresh') ||
      original.url?.includes('/auth/login')
    ) {
      return Promise.reject(error);
    }

    const refreshToken = tokenStore.getRefreshToken();
    if (!refreshToken) {
      handleAuthFailure();
      return Promise.reject(error);
    }

    original._retry = true;

    if (isRefreshing) {
      // Queue the request until the in-flight refresh finishes.
      return new Promise((resolve, reject) => {
        pendingQueue.push((token) => {
          if (!token) {
            reject(error);
            return;
          }
          original.headers = original.headers ?? {};
          (original.headers as Record<string, string>).Authorization = `Bearer ${token}`;
          resolve(axiosClient(original));
        });
      });
    }

    isRefreshing = true;
    try {
      const { data } = await axios.post<{
        accessToken: string;
        refreshToken: string;
      }>(`${API_BASE}/auth/refresh`, { refreshToken });

      tokenStore.setTokens(data.accessToken, data.refreshToken);
      flushQueue(data.accessToken);

      original.headers = original.headers ?? {};
      (original.headers as Record<string, string>).Authorization = `Bearer ${data.accessToken}`;
      return axiosClient(original);
    } catch (refreshError) {
      flushQueue(null);
      handleAuthFailure();
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  },
);
