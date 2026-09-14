import * as BackgroundFetch from 'expo-background-fetch';
import * as TaskManager from 'expo-task-manager';
import { runSync } from './syncService';

export const BACKGROUND_SYNC_TASK = 'flp-background-sync';

// Task definition must be registered at module scope (top-level), before the
// app renders, so the OS can invoke it even after a cold start.
TaskManager.defineTask(BACKGROUND_SYNC_TASK, async () => {
  try {
    const { flushed } = await runSync();
    return flushed > 0
      ? BackgroundFetch.BackgroundFetchResult.NewData
      : BackgroundFetch.BackgroundFetchResult.NoData;
  } catch {
    return BackgroundFetch.BackgroundFetchResult.Failed;
  }
});

/**
 * Registers the periodic background sync. Android schedules this with the
 * OS JobScheduler; the interval is a floor, not a guarantee.
 */
export async function registerBackgroundSync(): Promise<void> {
  try {
    const status = await BackgroundFetch.getStatusAsync();
    if (
      status === BackgroundFetch.BackgroundFetchStatus.Restricted ||
      status === BackgroundFetch.BackgroundFetchStatus.Denied
    ) {
      return;
    }
    const isRegistered = await TaskManager.isTaskRegisteredAsync(
      BACKGROUND_SYNC_TASK,
    );
    if (isRegistered) return;

    await BackgroundFetch.registerTaskAsync(BACKGROUND_SYNC_TASK, {
      minimumInterval: 15 * 60, // 15 minutes (Android minimum practical floor)
      stopOnTerminate: false,
      startOnBoot: true,
    });
  } catch {
    // Background fetch unavailable (e.g. Expo Go on some platforms) — the
    // foreground NetInfo sync still covers the common case.
  }
}

export async function unregisterBackgroundSync(): Promise<void> {
  try {
    const isRegistered = await TaskManager.isTaskRegisteredAsync(
      BACKGROUND_SYNC_TASK,
    );
    if (isRegistered) {
      await BackgroundFetch.unregisterTaskAsync(BACKGROUND_SYNC_TASK);
    }
  } catch {
    // ignore
  }
}
