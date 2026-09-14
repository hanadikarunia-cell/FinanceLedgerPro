import NetInfo from '@react-native-community/netinfo';
import { transactionsApi, dashboardApi, branchesApi } from '@/api';
import { outbox } from './outbox';
import { cache, cacheKeys } from './cache';
import type { QueryClient } from '@tanstack/react-query';

/**
 * Flushes the offline outbox and refreshes cached read-models.
 *
 * Two triggers wire into this:
 *   1. NetInfo connectivity transitions (foreground, immediate) — via start().
 *   2. expo-background-fetch (background/periodic) — via backgroundSync.ts.
 */

let syncing = false;
let queryClient: QueryClient | null = null;

/** Allow the app root to hand us the react-query client for cache invalidation. */
export function registerQueryClient(client: QueryClient): void {
  queryClient = client;
}

/** Drains the outbox; returns number of items successfully synced. */
export async function flushOutbox(): Promise<number> {
  if (syncing) return 0;
  syncing = true;
  let flushed = 0;
  try {
    const items = await outbox.list();
    for (const item of items) {
      try {
        await transactionsApi.create(item.payload);
        await outbox.remove(item.localId);
        flushed += 1;
      } catch (err) {
        // Leave it in the queue for the next attempt; record the reason.
        const message = err instanceof Error ? err.message : 'unknown error';
        await outbox.markFailure(item.localId, message);
        // Stop on the first failure to preserve FIFO ordering and avoid
        // hammering a server that is likely still unreachable.
        break;
      }
    }
  } finally {
    syncing = false;
  }
  return flushed;
}

/** Refreshes the read-model caches from the network (best-effort). */
export async function refreshCaches(): Promise<void> {
  try {
    const [dashboard, branches] = await Promise.all([
      dashboardApi.get(),
      branchesApi.list(),
    ]);
    await cache.set(cacheKeys.dashboard, dashboard);
    await cache.set(cacheKeys.branches, branches);
  } catch {
    // Offline or server error — keep whatever cache we already have.
  }
}

/**
 * Full sync cycle: flush writes first, then refresh reads, then let
 * react-query re-fetch anything currently mounted.
 */
export async function runSync(): Promise<{ flushed: number }> {
  const flushed = await flushOutbox();
  await refreshCaches();
  if (queryClient) {
    await queryClient.invalidateQueries();
  }
  return { flushed };
}

/**
 * Starts the foreground connectivity watcher. Whenever the device transitions
 * to a connected+reachable state, we run a sync. Returns an unsubscribe fn.
 */
export function startConnectivitySync(): () => void {
  let wasConnected = true;
  const unsubscribe = NetInfo.addEventListener((state) => {
    const isConnected = Boolean(state.isConnected && state.isInternetReachable);
    // Fire on the transition from offline -> online.
    if (isConnected && !wasConnected) {
      void runSync();
    }
    wasConnected = isConnected;
  });
  return unsubscribe;
}

export async function isOnline(): Promise<boolean> {
  const state = await NetInfo.fetch();
  return Boolean(state.isConnected && state.isInternetReachable);
}
