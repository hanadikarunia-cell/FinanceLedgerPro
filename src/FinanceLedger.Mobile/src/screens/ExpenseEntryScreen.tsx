import React from 'react';
import { StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { TransactionForm } from '@/components/TransactionForm';
import { SyncStatusBar } from '@/components/SyncStatusBar';

// Must match FinanceLedger.Domain.Constants.TransactionCategories.ExpenseCategories
// on the backend exactly — the API rejects any category outside this list.
const EXPENSE_CATEGORIES = [
  'Service',
  'Salaries',
  'Entertainment',
  'Office Utilities',
  'Taxes - PPN',
  'Taxes - PPH21',
  'Taxes - PPH25',
  'Taxes - PPH23',
  'Taxes - Other',
  'Car Debt',
  'Other',
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
