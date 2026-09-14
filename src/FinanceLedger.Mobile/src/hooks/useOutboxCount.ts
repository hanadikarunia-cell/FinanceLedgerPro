import { useEffect, useState } from 'react';
import { outbox } from '@/offline/outbox';

/** Live count of transactions pending offline sync. */
export function useOutboxCount(): number {
  const [count, setCount] = useState(0);
  useEffect(() => outbox.subscribe(setCount), []);
  return count;
}
