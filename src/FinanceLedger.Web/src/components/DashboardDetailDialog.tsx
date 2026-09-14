import { useTranslation } from 'react-i18next';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import type { GridColDef } from '@mui/x-data-grid';

import DataTable from '@/components/DataTable';
import LoadingSpinner from '@/components/LoadingSpinner';
import { StatusChip, TypeChip } from '@/components/StatusChip';
import { useDashboardBreakdown } from '@/hooks/useDashboard';
import { formatCurrency, formatDate, getErrorMessage } from '@/utils/format';
import type { DashboardMetric, PettyCashUserSummary, Transaction } from '@/types';

interface DashboardDetailDialogProps {
  metric: DashboardMetric | null;
  onClose: () => void;
}

export default function DashboardDetailDialog({ metric, onClose }: DashboardDetailDialogProps) {
  const { t } = useTranslation();
  const { data, isLoading, isError, error } = useDashboardBreakdown(metric);

  const isUserSummary = metric === 'pettyCashOutstanding';

  const transactionColumns: GridColDef<Transaction>[] = [
    {
      field: 'date',
      headerName: t('common.date'),
      width: 120,
      valueFormatter: (value) => formatDate(value as string),
    },
    {
      field: 'type',
      headerName: t('common.type'),
      width: 110,
      renderCell: (params) => <TypeChip type={params.row.type} />,
    },
    { field: 'category', headerName: t('common.category'), width: 150 },
    {
      field: 'amount',
      headerName: t('common.amount'),
      width: 140,
      valueFormatter: (value) => formatCurrency(value as number),
    },
    { field: 'createdByName', headerName: t('common.user'), width: 150 },
    {
      field: 'approvalStatus',
      headerName: t('common.status'),
      width: 130,
      renderCell: (params) => <StatusChip status={params.row.approvalStatus} />,
    },
  ];

  const summaryColumns: GridColDef<PettyCashUserSummary>[] = [
    { field: 'displayName', headerName: t('common.user'), flex: 1, minWidth: 160 },
    {
      field: 'issued',
      headerName: t('dashboard.issued'),
      width: 160,
      valueFormatter: (value) => formatCurrency(value as number),
    },
    {
      field: 'spent',
      headerName: t('dashboard.spent'),
      width: 160,
      valueFormatter: (value) => formatCurrency(value as number),
    },
    {
      field: 'outstanding',
      headerName: t('dashboard.outstanding'),
      width: 160,
      valueFormatter: (value) => formatCurrency(value as number),
    },
  ];

  return (
    <Dialog open={metric !== null} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{metric ? t(`dashboard.detailTitle_${metric}`) : ''}</DialogTitle>
      <DialogContent dividers>
        {isError && <Alert severity="error">{getErrorMessage(error)}</Alert>}
        {isLoading ? (
          <LoadingSpinner />
        ) : isUserSummary ? (
          <DataTable
            rows={data?.userSummaries ?? []}
            columns={summaryColumns}
            getRowId={(row) => row.userId}
            height={420}
          />
        ) : (
          <DataTable
            rows={data?.transactions ?? []}
            columns={transactionColumns}
            getRowId={(row) => row.id}
            height={420}
          />
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}
