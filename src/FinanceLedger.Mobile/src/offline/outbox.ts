import AsyncStorage from '@react-native-async-storage/async-storage';
import type { CreateTransactionRequest, OutboxItem, Transaction } from '@/types';

const OUTBOX_KEY = 'flp.outbox.transactions';

/** Simple non-crypto unique id for local records. */
function makeLocalId(): string {
  return `local_${Date.now()}_${Math.random().toString(36).slice(2, 9)}`;
}

type Listener = (count: number) => void;
const listeners = new Set<Listener>();

function notify(count: number): void {
  listeners.forEach((l) => l(count));
}

async function readAll(): Promise<OutboxItem[]> {
  const raw = await AsyncStorage.getItem(OUTBOX_KEY);
  if (!raw) return [];
  try {
    return JSON.parse(raw) as OutboxItem[];
  } catch {
    return [];
  }
}

async function writeAll(items: OutboxItem[]): Promise<void> {
  await AsyncStorage.setItem(OUTBOX_KEY, JSON.stringify(items));
  notify(items.length);
}

/**
 * Durable FIFO queue of transactions created while offline (or that failed to
 * POST). The sync service drains it when connectivity returns.
 */
export const outbox = {
  /** Subscribe to pending-count changes. Returns an unsubscribe fn. */
  subscribe(listener: Listener): () => void {
    listeners.add(listener);
    // Emit current value immediately.
    void this.count().then(listener);
    return () => {
      listeners.delete(listener);
    };
  },

  async count(): Promise<number> {
    return (await readAll()).length;
  },

  async list(): Promise<OutboxItem[]> {
    return readAll();
  },

  /** Enqueue a transaction payload. Returns an optimistic Transaction. */
  async enqueue(payload: CreateTransactionRequest): Promise<Transaction> {
    const items = await readAll();
    const item: OutboxItem = {
      localId: makeLocalId(),
      payload,
      createdAt: new Date().toISOString(),
      attempts: 0,
    };
    items.push(item);
    await writeAll(items);
    return { id: item.localId, ...payload };
  },

  async remove(localId: string): Promise<void> {
    const items = await readAll();
    await writeAll(items.filter((i) => i.localId !== localId));
  },

  async markFailure(localId: string, error: string): Promise<void> {
    const items = await readAll();
    const next = items.map((i) =>
      i.localId === localId
        ? { ...i, attempts: i.attempts + 1, lastError: error }
        : i,
    );
    await writeAll(next);
  },

  async clear(): Promise<void> {
    await writeAll([]);
  },
};
