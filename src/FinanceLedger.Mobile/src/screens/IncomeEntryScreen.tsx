import React from 'react';
import { StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { TransactionForm } from '@/components/TransactionForm';
import { SyncStatusBar } from '@/components/SyncStatusBar';

const INCOME_CATEGORIES = [
  'Sales',
  'Service Revenue',
  'Interest',
  'Refund',
  'Other Income',
];

export function IncomeEntryScreen() {
  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <SyncStatusBar />
      <TransactionForm type="Income" categories={INCOME_CATEGORIES} />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: '#fff' },
});
