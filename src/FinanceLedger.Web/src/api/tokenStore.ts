import type { User } from '@/types';

const ACCESS_KEY = 'flp.accessToken';
const REFRESH_KEY = 'flp.refreshToken';
const USER_KEY = 'flp.user';
// "View as" lives in sessionStorage on purpose: it ends when the tab closes, so an admin
// never comes back to a browser that is silently acting as someone else.
const ACT_AS_KEY = 'flp.actAs';

export interface ActAsState {
  user: User;
  canWrite: boolean;
}

export const tokenStore = {
  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_KEY);
  },
  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  },
  getUser(): User | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  },
  setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem(ACCESS_KEY, accessToken);
    localStorage.setItem(REFRESH_KEY, refreshToken);
  },
  setUser(user: User): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },
  getActAs(): ActAsState | null {
    try {
      const raw = sessionStorage.getItem(ACT_AS_KEY);
      return raw ? (JSON.parse(raw) as ActAsState) : null;
    } catch {
      return null;
    }
  },
  setActAs(state: ActAsState): void {
    sessionStorage.setItem(ACT_AS_KEY, JSON.stringify(state));
  },
  clearActAs(): void {
    sessionStorage.removeItem(ACT_AS_KEY);
  },
  clear(): void {
    sessionStorage.removeItem(ACT_AS_KEY);
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
  },
};
