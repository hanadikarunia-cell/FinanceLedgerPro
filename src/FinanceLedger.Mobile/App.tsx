import 'react-native-gesture-handler';
import React, { useEffect } from 'react';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { PaperProvider } from 'react-native-paper';
import { QueryClientProvider } from '@tanstack/react-query';

import { AuthProvider } from '@/auth/AuthContext';
import { RootNavigator } from '@/navigation/RootNavigator';
import { queryClient } from '@/lib/queryClient';
import { lightTheme } from '@/theme';
import {
  registerQueryClient,
  startConnectivitySync,
  runSync,
} from '@/offline/syncService';
import { registerBackgroundSync } from '@/offline/backgroundSync';

export default function App() {
  useEffect(() => {
    // Wire the sync service to react-query and start the connectivity watcher.
    registerQueryClient(queryClient);
    const unsubscribe = startConnectivitySync();

    // Attempt an initial sync + register periodic background sync (Android).
    void runSync();
    void registerBackgroundSync();

    return () => {
      unsubscribe();
    };
  }, []);

  return (
    <SafeAreaProvider>
      <QueryClientProvider client={queryClient}>
        <PaperProvider theme={lightTheme}>
          <AuthProvider>
            <StatusBar style="light" />
            <RootNavigator />
          </AuthProvider>
        </PaperProvider>
      </QueryClientProvider>
    </SafeAreaProvider>
  );
}
