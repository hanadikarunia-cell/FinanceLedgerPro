import React, { useCallback } from 'react';
import { Dimensions, RefreshControl, ScrollView, StyleSheet, View } from 'react-native';
import { ActivityIndicator, Card, Text } from 'react-native-paper';
import { SafeAreaView } from 'react-native-safe-area-context';
import { BarChart } from 'react-native-chart-kit';
import { StatCard } from '@/components/StatCard';
import { SyncStatusBar } from '@/components/SyncStatusBar';
import { useDashboard } from '@/hooks/useDashboard';
import { useAuth } from '@/auth/AuthContext';
import { palette } from '@/theme';
import type { MonthlyPoint } from '@/types';

const screenWidth = Dimensions.get('window').width;

function money(n: number): string {
  return `$${n.toLocaleString(undefined, { maximumFractionDigits: 0 })}`;
}

export function DashboardScreen() {
  const { user } = useAuth();
  const { data, isLoading, isError, refetch, isRefetching } = useDashboard();

  const onRefresh = useCallback(() => {
    void refetch();
  }, [refetch]);

  const trend = data?.trend ?? [];
  const chartData = {
    labels: trend.map((p: MonthlyPoint) => p.label),
    datasets: [
      { data: trend.map((p: MonthlyPoint) => p.income), color: () => palette.income },
      { data: trend.map((p: MonthlyPoint) => p.expense), color: () => palette.expense },
    ],
    legend: ['Income', 'Expense'],
  };

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <SyncStatusBar />
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={
          <RefreshControl refreshing={isRefetching} onRefresh={onRefresh} />
        }
      >
        <Text variant="titleLarge" style={styles.greeting}>
          Hello{user?.displayName ? `, ${user.displayName}` : ''}
        </Text>

        {isLoading ? (
          <ActivityIndicator style={styles.loader} size="large" />
        ) : isError && !data ? (
          <Card style={styles.errorCard}>
            <Card.Content>
              <Text>Could not load dashboard and no cached data available.</Text>
              <Text style={styles.hint}>Pull down to retry.</Text>
            </Card.Content>
          </Card>
        ) : (
          <>
            <View style={styles.row}>
              <StatCard
                label="Total Income"
                value={money(data?.totalIncome ?? 0)}
                accentColor={palette.income}
              />
              <StatCard
                label="Total Expense"
                value={money(data?.totalExpense ?? 0)}
                accentColor={palette.expense}
              />
            </View>
            <View style={styles.row}>
              <StatCard
                label="Net Balance"
                value={money(data?.netBalance ?? 0)}
                accentColor={palette.primary}
              />
              <StatCard
                label="Pending Approvals"
                value={String(data?.pendingApprovals ?? 0)}
                caption="Submitted / Draft"
              />
            </View>

            {trend.length > 0 ? (
              <Card style={styles.chartCard}>
                <Card.Title title="Income vs Expense" />
                <Card.Content>
                  <BarChart
                    data={chartData}
                    width={screenWidth - 64}
                    height={220}
                    yAxisLabel="$"
                    yAxisSuffix=""
                    fromZero
                    showValuesOnTopOfBars={false}
                    chartConfig={{
                      backgroundColor: '#fff',
                      backgroundGradientFrom: '#fff',
                      backgroundGradientTo: '#fff',
                      decimalPlaces: 0,
                      color: (opacity = 1) => `rgba(37, 99, 235, ${opacity})`,
                      labelColor: () => '#475569',
                      barPercentage: 0.6,
                    }}
                    style={styles.chart}
                  />
                </Card.Content>
              </Card>
            ) : null}
          </>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: '#f1f5f9' },
  content: { padding: 10, paddingBottom: 32 },
  greeting: { fontWeight: '700', margin: 8, marginBottom: 12 },
  row: { flexDirection: 'row' },
  loader: { marginTop: 48 },
  errorCard: { margin: 8 },
  hint: { opacity: 0.6, marginTop: 6 },
  chartCard: { margin: 6, marginTop: 12 },
  chart: { borderRadius: 12 },
});
