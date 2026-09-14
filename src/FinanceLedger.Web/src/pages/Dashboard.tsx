import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Grid from '@mui/material/Grid';
import Typography from '@mui/material/Typography';
import { useTheme } from '@mui/material/styles';

import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import PaidIcon from '@mui/icons-material/Paid';
import SavingsIcon from '@mui/icons-material/Savings';
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong';

import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ComposedChart,
  Legend,
  Line,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip as RTooltip,
  XAxis,
  YAxis,
} from 'recharts';

import StatCard from '@/components/StatCard';
import LoadingSpinner from '@/components/LoadingSpinner';
import { StatusChip, TypeChip } from '@/components/StatusChip';
import DataTable from '@/components/DataTable';
import DashboardDetailDialog from '@/components/DashboardDetailDialog';
import { useDashboard } from '@/hooks/useDashboard';
import { useAuth } from '@/context/AuthContext';
import { formatCurrency, formatDate, getErrorMessage } from '@/utils/format';
import type { GridColDef } from '@mui/x-data-grid';
import type { DashboardMetric, Transaction } from '@/types';

const PIE_COLORS = ['#1976d2', '#00897b', '#ed6c02', '#9c27b0', '#d32f2f', '#0288d1'];

export default function Dashboard() {
  const { t } = useTranslation();
  const theme = useTheme();
  const { isManager } = useAuth();
  const { data, isLoading, isError, error } = useDashboard();
  const [selectedMetric, setSelectedMetric] = useState<DashboardMetric | null>(null);

  const monthlySeries = useMemo(() => data?.monthlySeries ?? [], [data]);

  const recentColumns: GridColDef<Transaction>[] = [
    {
      field: 'date',
      headerName: t('common.date'),
      width: 130,
      valueFormatter: (value) => formatDate(value as string),
    },
    {
      field: 'type',
      headerName: t('common.type'),
      width: 120,
      renderCell: (params) => <TypeChip type={params.row.type} />,
    },
    { field: 'category', headerName: t('common.category'), flex: 1, minWidth: 140 },
    {
      field: 'amount',
      headerName: t('common.amount'),
      width: 140,
      valueFormatter: (value) => formatCurrency(value as number),
    },
    {
      field: 'approvalStatus',
      headerName: t('common.status'),
      width: 130,
      renderCell: (params) => <StatusChip status={params.row.approvalStatus} />,
    },
  ];

  if (isLoading) return <LoadingSpinner label={t('dashboard.loadingDashboard')} fullHeight />;
  if (isError)
    return (
      <Alert severity="error">{getErrorMessage(error, t('dashboard.failedToLoad'))}</Alert>
    );
  if (!data) return null;

  return (
    <Box>
      <Grid container spacing={3}>
        {isManager && (
          <Grid item xs={12} sm={6} lg={3}>
            <StatCard
              title={t('dashboard.totalIncome')}
              value={formatCurrency(data.totalIncome)}
              icon={<TrendingUpIcon />}
              color="success"
              onClick={() => setSelectedMetric('income')}
            />
          </Grid>
        )}
        {isManager && (
          <Grid item xs={12} sm={6} lg={3}>
            <StatCard
              title={t('dashboard.totalExpenses')}
              value={formatCurrency(data.totalExpense)}
              icon={<TrendingDownIcon />}
              color="warning"
              onClick={() => setSelectedMetric('expense')}
            />
          </Grid>
        )}
        {isManager && (
          <Grid item xs={12} sm={6} lg={3}>
            <StatCard
              title={t('dashboard.balance')}
              value={formatCurrency(data.netBalance)}
              icon={<AccountBalanceIcon />}
              color="primary"
              onClick={() => setSelectedMetric('balance')}
            />
          </Grid>
        )}
        <Grid item xs={12} sm={6} lg={3}>
          <StatCard
            title={t('pettyCash.totalIssued')}
            value={formatCurrency(data.totalPettyCashIssued)}
            icon={<PaidIcon />}
            color="info"
            onClick={() => setSelectedMetric('pettyCashIssued')}
          />
        </Grid>
        <Grid item xs={12} sm={6} lg={3}>
          <StatCard
            title={t('pettyCash.totalExpenses')}
            value={formatCurrency(data.totalPettyCashExpenses)}
            icon={<ReceiptLongIcon />}
            color="warning"
            onClick={() => setSelectedMetric('pettyCashExpenses')}
          />
        </Grid>
        <Grid item xs={12} sm={6} lg={3}>
          <StatCard
            title={t('pettyCash.outstanding')}
            value={formatCurrency(data.totalPettyCashOutstanding)}
            icon={<SavingsIcon />}
            color="info"
            onClick={() => setSelectedMetric('pettyCashOutstanding')}
          />
        </Grid>

        {/* Monthly income vs expense */}
        <Grid item xs={12} lg={8}>
          <Card sx={{ height: '100%' }}>
            <CardHeader title={t('dashboard.incomeVsExpense')} />
            <CardContent>
              <Box sx={{ width: '100%', height: 320 }}>
                <ResponsiveContainer>
                  <ComposedChart data={monthlySeries}>
                    <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
                    <XAxis dataKey="label" stroke={theme.palette.text.secondary} />
                    <YAxis stroke={theme.palette.text.secondary} />
                    <RTooltip
                      contentStyle={{
                        background: theme.palette.background.paper,
                        border: `1px solid ${theme.palette.divider}`,
                        borderRadius: 8,
                      }}
                      formatter={(v: number) => formatCurrency(v)}
                    />
                    <Legend />
                    <Bar
                      dataKey="income"
                      name={t('enums.type.Income')}
                      fill={theme.palette.success.main}
                      radius={[4, 4, 0, 0]}
                      barSize={22}
                    />
                    <Bar
                      dataKey="expense"
                      name={t('enums.type.Expense')}
                      fill={theme.palette.warning.main}
                      radius={[4, 4, 0, 0]}
                      barSize={22}
                    />
                    <Line
                      type="monotone"
                      dataKey="income"
                      name={t('enums.type.Income')}
                      stroke={theme.palette.primary.main}
                      strokeWidth={2}
                      dot={false}
                    />
                  </ComposedChart>
                </ResponsiveContainer>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Top categories pie */}
        <Grid item xs={12} lg={4}>
          <Card sx={{ height: '100%' }}>
            <CardHeader title={t('dashboard.topCategories')} />
            <CardContent>
              <Box sx={{ width: '100%', height: 320 }}>
                {data.topCategories.length === 0 ? (
                  <Typography color="text.secondary">{t('common.noDataAvailable')}</Typography>
                ) : (
                  <ResponsiveContainer>
                    <PieChart>
                      <Pie
                        data={data.topCategories}
                        dataKey="amount"
                        nameKey="category"
                        cx="50%"
                        cy="50%"
                        outerRadius={110}
                        label={(entry) => entry.category}
                      >
                        {data.topCategories.map((_, i) => (
                          <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
                        ))}
                      </Pie>
                      <RTooltip formatter={(v: number) => formatCurrency(v)} />
                    </PieChart>
                  </ResponsiveContainer>
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Expense breakdown bar (secondary chart to satisfy bar+line requirement) */}
        <Grid item xs={12} lg={4}>
          <Card sx={{ height: '100%' }}>
            <CardHeader title={t('dashboard.expenseByMonth')} />
            <CardContent>
              <Box sx={{ width: '100%', height: 260 }}>
                <ResponsiveContainer>
                  <BarChart data={monthlySeries}>
                    <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
                    <XAxis dataKey="label" stroke={theme.palette.text.secondary} />
                    <YAxis stroke={theme.palette.text.secondary} />
                    <RTooltip formatter={(v: number) => formatCurrency(v)} />
                    <Bar
                      dataKey="expense"
                      name={t('enums.type.Expense')}
                      fill={theme.palette.warning.main}
                      radius={[4, 4, 0, 0]}
                    />
                  </BarChart>
                </ResponsiveContainer>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Recent transactions */}
        <Grid item xs={12} lg={8}>
          <Card sx={{ height: '100%' }}>
            <CardHeader title={t('dashboard.recentTransactions')} />
            <CardContent>
              <DataTable
                rows={data.recent}
                columns={recentColumns}
                height={360}
                hideFooter
                getRowId={(row) => row.id}
              />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <DashboardDetailDialog metric={selectedMetric} onClose={() => setSelectedMetric(null)} />
    </Box>
  );
}
