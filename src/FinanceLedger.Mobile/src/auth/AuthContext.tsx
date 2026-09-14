import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { authApi, setSessionExpiredHandler } from '@/api';
import { tokenStorage } from './tokenStorage';
import { cache } from '@/offline/cache';
import type { LoginRequest, UserProfile } from '@/types';

interface AuthState {
  user: UserProfile | null;
  isAuthenticated: boolean;
  isBootstrapping: boolean;
  isLoggingIn: boolean;
  error: string | null;
  login: (creds: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [isBootstrapping, setIsBootstrapping] = useState(true);
  const [isLoggingIn, setIsLoggingIn] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const logout = useCallback(async () => {
    await tokenStorage.clear();
    await cache.clearAll();
    setUser(null);
  }, []);

  // On cold start: if we have a refresh token, treat the session as restorable.
  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const [refresh, cachedUser] = await Promise.all([
          tokenStorage.getRefreshToken(),
          tokenStorage.getUser(),
        ]);
        if (mounted && refresh) {
          setUser(cachedUser);
        }
      } finally {
        if (mounted) setIsBootstrapping(false);
      }
    })();
    return () => {
      mounted = false;
    };
  }, []);

  // Wire the axios interceptor's "session dead" callback to a real logout.
  useEffect(() => {
    setSessionExpiredHandler(() => {
      void logout();
    });
  }, [logout]);

  const login = useCallback(async (creds: LoginRequest) => {
    setIsLoggingIn(true);
    setError(null);
    try {
      const res = await authApi.login(creds);
      await tokenStorage.saveTokens({
        accessToken: res.accessToken,
        refreshToken: res.refreshToken,
      });
      await tokenStorage.saveUser(res.user);
      setUser(res.user);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Login failed. Please try again.';
      setError(message);
      throw err;
    } finally {
      setIsLoggingIn(false);
    }
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isBootstrapping,
      isLoggingIn,
      error,
      login,
      logout,
    }),
    [user, isBootstrapping, isLoggingIn, error, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
