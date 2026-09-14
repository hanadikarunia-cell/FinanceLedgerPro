import Constants from 'expo-constants';

/**
 * Resolves the API base URL from (in priority order):
 *   1. EXPO_PUBLIC_API_BASE_URL env var (injected at build time)
 *   2. app.json -> expo.extra.apiBaseUrl
 *   3. a safe localhost fallback for dev
 */
function resolveApiBaseUrl(): string {
  const fromEnv = process.env.EXPO_PUBLIC_API_BASE_URL;
  const fromExtra = (Constants.expoConfig?.extra as { apiBaseUrl?: string } | undefined)
    ?.apiBaseUrl;
  return fromEnv ?? fromExtra ?? 'http://10.0.2.2:5000';
}

export const API_BASE_URL = resolveApiBaseUrl();

/** Versioned REST root, per shared contract. */
export const API_ROOT = `${API_BASE_URL}/api/v1`;
