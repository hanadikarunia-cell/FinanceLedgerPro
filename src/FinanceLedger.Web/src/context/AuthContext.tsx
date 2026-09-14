import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { authApi } from '@/api/auth';
import { tokenStore } from '@/api/tokenStore';
import type { LoginRequest, User, UserRole } from '@/types';

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  login: (payload: LoginRequest) => Promise<User>;
  logout: () => void;
  hasRole: (role: UserRole) => boolean;
  isManager: boolean;
  updateUser: (user: User) => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => tokenStore.getUser());

  const login = useCallback(async (payload: LoginRequest): Promise<User> => {
    const res = await authApi.login(payload);
    tokenStore.setTokens(res.accessToken, res.refreshToken);
    tokenStore.setUser(res.user);
    setUser(res.user);
    return res.user;
  }, []);

  const logout = useCallback(() => {
    tokenStore.clear();
    setUser(null);
  }, []);

  const updateUser = useCallback((next: User) => {
    tokenStore.setUser(next);
    setUser(next);
  }, []);

  const hasRole = useCallback((role: UserRole) => user?.role === role, [user]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: !!user && !!tokenStore.getAccessToken(),
      login,
      logout,
      hasRole,
      isManager: user?.role === 'Manager',
      updateUser,
    }),
    [user, login, logout, hasRole, updateUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
