import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { authApi } from '@/api/auth';
import { tokenStore } from '@/api/tokenStore';
import type { LoginRequest, User, UserRole } from '@/types';

interface ActingAsView {
  /** The admin who is really signed in. */
  realUser: User;
  canWrite: boolean;
}

interface AuthContextValue {
  /** The effective user: the acted-as user during "View as", otherwise the signed-in user. */
  user: User | null;
  isAuthenticated: boolean;
  login: (payload: LoginRequest) => Promise<User>;
  logout: () => void;
  hasRole: (role: UserRole) => boolean;
  /** Effective role is Site Admin. */
  isManager: boolean;
  /** Effective role is Application Admin. */
  isAppAdmin: boolean;
  updateUser: (user: User) => void;

  /** The signed-in user, ignoring "View as". */
  realUser: User | null;
  /** True when the signed-in user is allowed to use "View as". */
  canViewAs: boolean;
  actingAs: ActingAsView | null;
  startActingAs: (target: User, canWrite: boolean) => Promise<void>;
  setActingCanWrite: (canWrite: boolean) => void;
  stopActingAs: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [realUser, setRealUser] = useState<User | null>(() => tokenStore.getUser());
  const [actAs, setActAs] = useState(() => tokenStore.getActAs());

  // Everything cached belongs to whoever we were a moment ago; drop it whenever the
  // effective identity changes so one site's data is never shown under another's view.
  const resetCaches = useCallback(() => {
    queryClient.clear();
  }, [queryClient]);

  const login = useCallback(
    async (payload: LoginRequest): Promise<User> => {
      const res = await authApi.login(payload);
      tokenStore.clear();
      tokenStore.setTokens(res.accessToken, res.refreshToken);
      tokenStore.setUser(res.user);
      resetCaches();
      setActAs(null);
      setRealUser(res.user);
      return res.user;
    },
    [resetCaches],
  );

  const logout = useCallback(() => {
    tokenStore.clear();
    resetCaches();
    setActAs(null);
    setRealUser(null);
  }, [resetCaches]);

  const updateUser = useCallback((next: User) => {
    tokenStore.setUser(next);
    setRealUser(next);
  }, []);

  const startActingAs = useCallback(
    async (target: User, canWrite: boolean) => {
      const state = { user: target, canWrite };
      tokenStore.setActAs(state);
      try {
        // The server is the authority: it rejects anyone this admin may not act as.
        const me = await authApi.me();
        const next = { user: me.user, canWrite: me.actingAs?.canWrite ?? canWrite };
        tokenStore.setActAs(next);
        resetCaches();
        setActAs(next);
      } catch (err) {
        tokenStore.clearActAs();
        throw err;
      }
    },
    [resetCaches],
  );

  const setActingCanWrite = useCallback((canWrite: boolean) => {
    const current = tokenStore.getActAs();
    if (!current) return;
    const next = { ...current, canWrite };
    tokenStore.setActAs(next);
    setActAs(next);
  }, []);

  const stopActingAs = useCallback(() => {
    tokenStore.clearActAs();
    resetCaches();
    setActAs(null);
  }, [resetCaches]);

  const value = useMemo<AuthContextValue>(() => {
    const user = actAs?.user ?? realUser;
    return {
      user,
      isAuthenticated: !!realUser && !!tokenStore.getAccessToken(),
      login,
      logout,
      hasRole: (role: UserRole) => user?.role === role,
      isManager: user?.role === 'Manager',
      isAppAdmin: user?.role === 'AppAdmin',
      updateUser,
      realUser,
      canViewAs: realUser?.role === 'AppAdmin' || realUser?.role === 'Manager',
      actingAs: actAs && realUser ? { realUser, canWrite: actAs.canWrite } : null,
      startActingAs,
      setActingCanWrite,
      stopActingAs,
    };
  }, [
    actAs,
    realUser,
    login,
    logout,
    updateUser,
    startActingAs,
    setActingCanWrite,
    stopActingAs,
  ]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
