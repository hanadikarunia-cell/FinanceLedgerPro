import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Grid from '@mui/material/Grid';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useTheme } from '@mui/material/styles';

import GridOnIcon from '@mui/icons-material/GridOn';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import DescriptionIcon from '@mui/icons-material/Description';

import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip as RTooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { GridColDef } from '@mui/x-data-grid';

import DataTable from '@/components/DataTable';
import LoadingSpinner from '@/components/LoadingSpinner';
import StatCard from '@/components/StatCard';

import { useReports } from '@/hooks/useReports';
import { useBranches } from '@/hooks/useBranches';
import { useUsers } from '@/hooks/useUsers';
import { useAuth } from '@/context/AuthContext';
import { exportApi } from '@/api/export';
import type {
  ExportFormat,
  ReportFilters,
  ReportPeriod,
  Transaction,
} from '@/types';
import { StatusChip, TypeChip } from '@/components/StatusChip';
import { formatCurrency, formatDate, getErrorMessage } from '@/utils/format';

import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import ShowChartIcon from '@mui/icons-material/ShowChart';

const PERIODS: ReportPeriod[] = ['daily', 'monthly', 'yearly'];

export default function Reports() {
  const { t } = useTranslation();
  const theme = useTheme();
  const { isManager } = useAuth();
  const { data: branches = [] } = useBranches();
  const { data: users = [] } = useUsers();

  const [period, setPeriod] = useState<ReportPeriod>('monthly');
  const [filters, setFilters] = useState<ReportFilters>({});
  const [exporting, setExporting] = useState<ExportFormat | null>(null);
  const [exportError, setExportError] = useState<string | null>(null);

  const { data, isLoading, isError, error } = useReports(period, filters);

  // Merge income/expense category breakdowns into one chart series keyed by category.
  const categorySeries = useMemo(() => {
    if (!data) return [];
    const map = new Map<string, { category: string; income: number; expense: number }>();
    data.incomeByCategory.forEach((c) =>
      map.set(c.category, { category: c.category, income: c.amount, expense: 0 }),
    );
    data.expenseByCategory.forEach((c) => {
      const existing = map.get(c.category);
      if (existing) existing.expense = c.amount;
      else map.set(c.category, { category: c.category, income: 0, expense: c.amount });
    });
    return Array.from(map.values());
  }, [data]);

  const columns: GridColDef<Transaction>[] = [
    {
      field: 'date',
      headerName: t('common.date'),
      width: 130,
      valueFormatter: (value) => formatDate(value as string),
    },
    {
      field: 'type',
      headerName: t('common.type'),
      width: 110,
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

  const handleExport = async (format: ExportFormat) => {
    setExportError(null);
    setExporting(format);
    try {
      await exportApi.download(format, filters, `report-${period}`);
    } catch (err) {
      setExportError(getErrorMessage(err, t('reports.exportFailed')));
    } finally {
      setExporting(null);
    }
  };

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        {t('reports.title')}
      </Typography>

      <Tabs
        value={period}
        onChange={(_, v: ReportPeriod) => setPeriod(v)}
        sx={{ mb: 3 }}
      >
        {PERIODS.map((p) => (
          <Tab key={p} value={p} label={t(`reports.${p}`)} />
        ))}
      </Tabs>

      {/* Filters + export */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                label={t('common.from')}
                type="date"
                fullWidth
                size="small"
                InputLabelProps={{ shrink: true }}
                value={filters.from ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, from: e.target.value || undefined }))
                }
              />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                label={t('common.to')}
                type="date"
                fullWidth
                size="small"
                InputLabelProps={{ shrink: true }}
                value={filters.to ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, to: e.target.value || undefined }))
                }
              />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                label={t('common.category')}
                fullWidth
                size="small"
                value={filters.category ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, category: e.target.value || undefined }))
                }
              />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label={t('common.branch')}
                fullWidth
                size="small"
                value={filters.branch ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, branch: e.target.value || undefined }))
                }
              >
                <MenuItem value="">{t('common.all')}</MenuItem>
                {branches.map((b) => (
                  <MenuItem key={b.id} value={b.id}>
                    {b.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            {isManager && (
              <Grid item xs={12} sm={6} md={2}>
                <TextField
                  select
                  label={t('common.user')}
                  fullWidth
                  size="small"
                  value={filters.userId ?? ''}
                  onChange={(e) =>
                    setFilters((f) => ({ ...f, userId: e.target.value || undefined }))
                  }
                >
                  <MenuItem value="">{t('common.all')}</MenuItem>
                  {users.map((u) => (
                    <MenuItem key={u.id} value={u.id}>
                      {u.displayName}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
            )}
            <Grid item xs={12} md={2}>
              <Button
                variant="outlined"
                fullWidth
                onClick={() => setFilters({})}
              >
                {t('common.clear')}
              </Button>
            </Grid>
          </Grid>

          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1.5}
            sx={{ mt: 2 }}
          >
            <Button
              variant="contained"
              color="success"
              startIcon={<GridOnIcon />}
              disabled={exporting !== null}
              onClick={() => handleExport('excel')}
            >
              {exporting === 'excel' ? t('reports.exporting') : t('reports.exportExcel')}
            </Button>
            <Button
              variant="contained"
              color="error"
              startIcon={<PictureAsPdfIcon />}
              disabled={exporting !== null}
              onClick={() => handleExport('pdf')}
            >
              {exporting === 'pdf' ? t('reports.exporting') : t('reports.exportPdf')}
            </Button>
            <Button
              variant="outlined"
              startIcon={<DescriptionIcon />}
              disabled={exporting !== null}
              onClick={() => handleExport('csv')}
            >
              {exporting === 'csv' ? t('reports.exporting') : t('reports.exportCsv')}
            </Button>
          </Stack>
          {exportError && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {exportError}
            </Alert>
          )}
        </CardContent>
      </Card>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('reports.failedToLoad'))}
        </Alert>
      )}

      {isLoading ? (
        <LoadingSpinner label={t('reports.buildingReport')} />
      ) : data ? (
        <>
          <Grid container spacing={3} sx={{ mb: 3 }}>
            <Grid item xs={12} sm={4}>
              <StatCard
                title={t('reports.totalIncome')}
                value={formatCurrency(data.totalIncome)}
                icon={<TrendingUpIcon />}
                color="success"
              />
            </Grid>
            <Grid item xs={12} sm={4}>
              <StatCard
                title={t('reports.totalExpense')}
                value={formatCurrency(data.totalExpense)}
                icon={<TrendingDownIcon />}
                color="warning"
              />
            </Grid>
            <Grid item xs={12} sm={4}>
              <StatCard
                title={t('reports.net')}
                value={formatCurrency(data.netBalance)}
                icon={<ShowChartIcon />}
                color={data.netBalance >= 0 ? 'info' : 'error'}
              />
            </Grid>
          </Grid>

          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Box sx={{ width: '100%', height: 340 }}>
                <ResponsiveContainer>
                  <BarChart data={categorySeries}>
                    <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
                    <XAxis dataKey="category" stroke={theme.palette.text.secondary} />
                    <YAxis stroke={theme.palette.text.secondary} />
                    <RTooltip formatter={(v: number) => formatCurrency(v)} />
                    <Legend />
                    <Bar
                      dataKey="income"
                      name={t('enums.type.Income')}
                      fill={theme.palette.success.main}
                      radius={[4, 4, 0, 0]}
                    />
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

          <Card>
            <CardContent>
              <DataTable
                rows={data.transactions}
                columns={columns}
                height={420}
                getRowId={(row) => row.id}
              />
            </CardContent>
          </Card>
        </>
      ) : null}
    </Box>
  );
}
