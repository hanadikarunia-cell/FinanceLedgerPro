import React, { useState } from 'react';
import { FlatList, StyleSheet, View } from 'react-native';
import {
  ActivityIndicator,
  Card,
  Divider,
  IconButton,
  List,
  Text,
} from 'react-native-paper';
import { SafeAreaView } from 'react-native-safe-area-context';
import { SyncStatusBar } from '@/components/SyncStatusBar';
import { useMonthlyReport } from '@/hooks/useMonthlyReport';
import { palette } from '@/theme';
import type { MonthlyReportLine } from '@/types';

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

function money(n: number): string {
  return `$${n.toLocaleString(undefined, { maximumFractionDigits: 2 })}`;
}

export function ReportsScreen() {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1); // 1-12

  const { data, isLoading, isError } = useMonthlyReport(year, month);

  const shiftMonth = (delta: number) => {
    let m = month + delta;
    let y = year;
    if (m < 1) {
      m = 12;
      y -= 1;
    } else if (m > 12) {
      m = 1;
      y += 1;
    }
    setMonth(m);
    setYear(y);
  };

  const renderLine = ({ item }: { item: MonthlyReportLine }) => (
    <List.Item
      title={item.category}
      description={item.type}
      right={() => (
        <Text
          style={{
            color: item.type === 'Income' ? palette.income : palette.expense,
            alignSelf: 'center',
            fontWeight: '600',
          }}
        >
          {money(item.amount)}
        </Text>
      )}
    />
  );

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <SyncStatusBar />

      <View style={styles.periodBar}>
        <IconButton icon="chevron-left" onPress={() => shiftMonth(-1)} />
        <Text variant="titleMedium" style={styles.period}>
          {MONTHS[month - 1]} {year}
        </Text>
        <IconButton icon="chevron-right" onPress={() => shiftMonth(1)} />
      </View>

      {isLoading ? (
        <ActivityIndicator style={styles.loader} size="large" />
      ) : isError && !data ? (
        <Card style={styles.card}>
          <Card.Content>
            <Text>No report available for this period (offline).</Text>
          </Card.Content>
        </Card>
      ) : (
        <>
          <View style={styles.summaryRow}>
            <Card style={styles.summaryCard}>
              <Card.Content>
                <Text variant="labelMedium">Income</Text>
                <Text variant="titleMedium" style={{ color: palette.income }}>
                  {money(data?.totalIncome ?? 0)}
                </Text>
              </Card.Content>
            </Card>
            <Card style={styles.summaryCard}>
              <Card.Content>
                <Text variant="labelMedium">Expense</Text>
                <Text variant="titleMedium" style={{ color: palette.expense }}>
                  {money(data?.totalExpense ?? 0)}
                </Text>
              </Card.Content>
            </Card>
            <Card style={styles.summaryCard}>
              <Card.Content>
                <Text variant="labelMedium">Net</Text>
                <Text variant="titleMedium" style={{ color: palette.primary }}>
                  {money(data?.net ?? 0)}
                </Text>
              </Card.Content>
            </Card>
          </View>

          <Divider />
          <FlatList
            data={data?.lines ?? []}
            keyExtractor={(item, i) => `${item.category}-${item.type}-${i}`}
            renderItem={renderLine}
            ItemSeparatorComponent={Divider}
            ListEmptyComponent={
              <Text style={styles.empty}>No line items for this period.</Text>
            }
            contentContainerStyle={styles.list}
          />
        </>
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: '#f1f5f9' },
  periodBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 8,
  },
  period: { fontWeight: '700' },
  loader: { marginTop: 48 },
  card: { margin: 16 },
  summaryRow: { flexDirection: 'row', padding: 8, gap: 8 },
  summaryCard: { flex: 1 },
  list: { paddingBottom: 32 },
  empty: { textAlign: 'center', opacity: 0.6, marginTop: 32 },
});
