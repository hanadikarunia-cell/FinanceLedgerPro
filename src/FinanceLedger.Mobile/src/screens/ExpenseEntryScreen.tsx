import React from 'react';
import { StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { TransactionForm } from '@/components/TransactionForm';
import { SyncStatusBar } from '@/components/SyncStatusBar';

const EXPENSE_CATEGORIES = [
  'Rent',
  'Salaries',
  'Utilities',
  'Supplies',
  'Travel',
  'Marketing',
  'Other Expense',
];

export function ExpenseEntryScreen() {
  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <SyncStatusBar />
      <TransactionForm type="Expense" categories={EXPENSE_CATEGORIES} />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: '#fff' },
});
