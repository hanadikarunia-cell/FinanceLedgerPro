import React, { useEffect, useState } from 'react';
import { StyleSheet, View } from 'react-native';
import { Banner, Text, ActivityIndicator } from 'react-native-paper';
import NetInfo from '@react-native-community/netinfo';
import { useOutboxCount } from '@/hooks/useOutboxCount';
import { runSync } from '@/offline/syncService';
import { palette } from '@/theme';

/**
 * A slim status strip showing connectivity + pending offline items, with a
 * manual "Sync now" action. Hidden entirely when online and nothing pending.
 */
export function SyncStatusBar() {
  const pending = useOutboxCount();
  const [online, setOnline] = useState(true);
  const [syncing, setSyncing] = useState(false);

  useEffect(() => {
    const unsub = NetInfo.addEventListener((state) => {
      setOnline(Boolean(state.isConnected && state.isInternetReachable));
    });
    return unsub;
  }, []);

  if (online && pending === 0) return null;

  const onSyncNow = async () => {
    setSyncing(true);
    try {
      await runSync();
    } finally {
      setSyncing(false);
    }
  };

  const message = !online
    ? `Offline${pending > 0 ? ` — ${pending} item(s) queued` : ''}`
    : `${pending} item(s) pending sync`;

  return (
    <Banner
      visible
      icon={() =>
        syncing ? (
          <ActivityIndicator size={18} />
        ) : (
          <View
            style={[
              styles.dot,
              { backgroundColor: online ? palette.income : palette.expense },
            ]}
          />
        )
      }
      actions={
        online && pending > 0
          ? [{ label: syncing ? 'Syncing…' : 'Sync now', onPress: onSyncNow }]
          : []
      }
    >
      <Text>{message}</Text>
    </Banner>
  );
}

const styles = StyleSheet.create({
  dot: {
    width: 12,
    height: 12,
    borderRadius: 6,
  },
});
