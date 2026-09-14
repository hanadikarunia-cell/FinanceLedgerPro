import * as SecureStore from 'expo-secure-store';
import type { AuthTokens, UserProfile } from '@/types';

const ACCESS_KEY = 'flp.accessToken';
const REFRESH_KEY = 'flp.refreshToken';
const USER_KEY = 'flp.userProfile';

/**
 * Secure, OS-backed storage for JWTs and the cached user profile.
 * Uses the Android Keystore via expo-secure-store. Never store tokens in
 * AsyncStorage (that layer is for non-sensitive offline cache only).
 */
export const tokenStorage = {
  async saveTokens(tokens: AuthTokens): Promise<void> {
    await Promise.all([
      SecureStore.setItemAsync(ACCESS_KEY, tokens.accessToken),
      SecureStore.setItemAsync(REFRESH_KEY, tokens.refreshToken),
    ]);
  },

  async getAccessToken(): Promise<string | null> {
    return SecureStore.getItemAsync(ACCESS_KEY);
  },

  async getRefreshToken(): Promise<string | null> {
    return SecureStore.getItemAsync(REFRESH_KEY);
  },

  async saveUser(user: UserProfile): Promise<void> {
    await SecureStore.setItemAsync(USER_KEY, JSON.stringify(user));
  },

  async getUser(): Promise<UserProfile | null> {
    const raw = await SecureStore.getItemAsync(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as UserProfile;
    } catch {
      return null;
    }
  },

  async clear(): Promise<void> {
    await Promise.all([
      SecureStore.deleteItemAsync(ACCESS_KEY),
      SecureStore.deleteItemAsync(REFRESH_KEY),
      SecureStore.deleteItemAsync(USER_KEY),
    ]);
  },
};
