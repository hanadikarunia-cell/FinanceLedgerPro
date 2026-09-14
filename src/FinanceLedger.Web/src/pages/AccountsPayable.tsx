import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Grid from '@mui/material/Grid';
import Typography from '@mui/material/Typography';

import type { GridColDef } from '@mui/x-data-grid';
import DataTable from '@/components/DataTable';
import StatCard from '@/components/StatCard';
import AccountBalanceWalletIcon from '@mui/icons-material/AccountBalanceWallet';
import { useBranches } from '@/hooks/useBranches';
import { useCars } from '@/hooks/useCars';
import type { Car } from '@/types';
import { formatCurrency, formatDate, getErrorMessage } from '@/utils/format';

export default function AccountsPayable() {
  const { t } = useTranslation();
  const { data: cars = [], isLoading, isError, error } = useCars();
  const { data: branches = [] } = useBranches();

  const branchNameById = useMemo(() => new Map(branches.map((b) => [b.id, b.name])), [branches]);

  const payables = useMemo(
    () =>
      cars
        .filter((c) => c.remainingDebt > 0)
        .sort((a, b) => b.remainingDebt - a.remainingDebt),
    [cars],
  );

  const totalOutstanding = useMemo(
    () => payables.reduce((sum, c) => sum + c.remainingDebt, 0),
    [payables],
  );

  const columns: GridColDef<Car>[] = [
    { field: 'client', headerName: t('cars.client'), flex: 1, minWidth: 150 },
    { field: 'plateNumber', headerName: t('cars.plateNumber'), width: 130 },
    { field: 'model', headerName: t('cars.model'), flex: 1, minWidth: 140 },
    {
      field: 'branch',
      headerName: t('common.branch'),
      width: 120,
      valueFormatter: (value) => branchNameById.get(value as string) ?? (value as string),
    },
    {
      field: 'initialDebt',
      headerName: t('cars.initialDebt'),
      width: 150,
      valueFormatter: (value) => formatCurrency(value as number),
    },
    {
      field: 'remainingDebt',
      headerName: t('accountsPayable.outstanding'),
      width: 160,
      valueFormatter: (value) => formatCurrency(value as number),
    },
    {
      field: 'contractEndDate',
      headerName: t('cars.contractEndDate'),
      width: 130,
      valueFormatter: (value) => formatDate(value as string),
    },
  ];

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 1 }}>
        {t('accountsPayable.title')}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        {t('accountsPayable.subtitle')}
      </Typography>

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={4}>
          <StatCard
            title={t('accountsPayable.totalOutstanding')}
            value={formatCurrency(totalOutstanding)}
            icon={<AccountBalanceWalletIcon />}
            color="warning"
          />
        </Grid>
      </Grid>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('accountsPayable.failedToLoad'))}
        </Alert>
      )}

      <Card>
        <CardContent>
          <DataTable
            rows={payables}
            columns={columns}
            loading={isLoading}
            getRowId={(row) => row.id}
          />
        </CardContent>
      </Card>
    </Box>
  );
}
