import AsyncStorage from '@react-native-async-storage/async-storage';

/**
 * Namespaced, typed wrapper over AsyncStorage for non-sensitive offline cache.
 * Every cached blob is stored with a timestamp so callers can reason about
 * staleness if they wish.
 */
export interface CachedEnvelope<T> {
  data: T;
  cachedAt: string;
}

const PREFIX = 'flp.cache.';

export const cacheKeys = {
  dashboard: 'dashboard',
  branches: 'branches',
  transactions: (type: string) => `transactions.${type}`,
  monthlyReport: (year: number, month: number) => `report.${year}.${month}`,
} as const;

export const cache = {
  async set<T>(key: string, data: T): Promise<void> {
    const envelope: CachedEnvelope<T> = {
      data,
      cachedAt: new Date().toISOString(),
    };
    await AsyncStorage.setItem(PREFIX + key, JSON.stringify(envelope));
  },

  async get<T>(key: string): Promise<T | null> {
    const raw = await AsyncStorage.getItem(PREFIX + key);
    if (!raw) return null;
    try {
      const envelope = JSON.parse(raw) as CachedEnvelope<T>;
      return envelope.data;
    } catch {
      return null;
    }
  },

  async getEnvelope<T>(key: string): Promise<CachedEnvelope<T> | null> {
    const raw = await AsyncStorage.getItem(PREFIX + key);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as CachedEnvelope<T>;
    } catch {
      return null;
    }
  },

  async remove(key: string): Promise<void> {
    await AsyncStorage.removeItem(PREFIX + key);
  },

  /** Clears all namespaced cache entries (used on logout). */
  async clearAll(): Promise<void> {
    const keys = await AsyncStorage.getAllKeys();
    const ours = keys.filter((k) => k.startsWith(PREFIX));
    if (ours.length) await AsyncStorage.multiRemove(ours);
  },
};
