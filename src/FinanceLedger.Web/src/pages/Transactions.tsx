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
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import IconButton from '@mui/material/IconButton';

import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import PaidIcon from '@mui/icons-material/Paid';

import StatCard from '@/components/StatCard';
import { usePettyCashBalance } from '@/hooks/usePettyCashBalance';

import type {
  GridColDef,
  GridPaginationModel,
} from '@mui/x-data-grid';

import DataTable from '@/components/DataTable';
import ConfirmDialog from '@/components/ConfirmDialog';
import { StatusChip, TypeChip } from '@/components/StatusChip';
import TransactionFormDialog from './transactions/TransactionFormDialog';

import {
  useApproveTransaction,
  useDeleteTransaction,
  useRejectTransaction,
  useTransactions,
} from '@/hooks/useTransactions';
import { useBranches } from '@/hooks/useBranches';
import { useAuth } from '@/context/AuthContext';
import type {
  ApprovalStatus,
  Transaction,
  TransactionFilters,
  TransactionType,
} from '@/types';
import { formatCurrency, formatDate, getErrorMessage } from '@/utils/format';

const TYPES: TransactionType[] = ['Income', 'Expense'];
const STATUSES: ApprovalStatus[] = ['Draft', 'Submitted', 'Approved', 'Rejected'];

export default function Transactions() {
  const { t } = useTranslation();
  const { isManager } = useAuth();
  const { data: branches = [] } = useBranches();
  const { data: pettyCashBalance } = usePettyCashBalance();

  const [filters, setFilters] = useState<TransactionFilters>({
    page: 1,
    pageSize: 25,
  });

  const { data, isLoading, isError, error, isFetching } = useTransactions(filters);

  const approveMut = useApproveTransaction();
  const rejectMut = useRejectTransaction();
  const deleteMut = useDeleteTransaction();

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Transaction | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Transaction | null>(null);
  const [rejectTarget, setRejectTarget] = useState<Transaction | null>(null);

  const paginationModel: GridPaginationModel = {
    page: (filters.page ?? 1) - 1,
    pageSize: filters.pageSize ?? 25,
  };

  const updateFilter = (patch: Partial<TransactionFilters>) =>
    setFilters((prev) => ({ ...prev, ...patch, page: 1 }));

  const branchNameById = useMemo(
    () => new Map(branches.map((b) => [b.id, b.name])),
    [branches],
  );

  const canEdit = (t: Transaction) => {
    // Users cannot edit approved transactions; managers can always edit.
    if (isManager) return true;
    return t.approvalStatus !== 'Approved';
  };

  const columns: GridColDef<Transaction>[] = useMemo(
    () => [
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
      { field: 'category', headerName: t('common.category'), width: 140 },
      {
        field: 'amount',
        headerName: t('common.amount'),
        width: 130,
        valueFormatter: (value) => formatCurrency(value as number),
      },
      {
        field: 'branch',
        headerName: t('common.branch'),
        width: 140,
        valueFormatter: (value) => branchNameById.get(value as string) ?? (value as string),
      },
      { field: 'createdByName', headerName: t('common.user'), width: 140 },
      {
        field: 'approvalStatus',
        headerName: t('common.status'),
        width: 130,
        renderCell: (params) => <StatusChip status={params.row.approvalStatus} />,
      },
      {
        field: 'actions',
        headerName: t('common.actions'),
        width: 200,
        sortable: false,
        filterable: false,
        renderCell: (params) => {
          const row = params.row;
          const pendingApproval =
            row.approvalStatus === 'Submitted' || row.approvalStatus === 'Draft';
          return (
            <Stack direction="row" spacing={0.5}>
              {isManager && pendingApproval && (
                <>
                  <Tooltip title={t('transactions.approve')}>
                    <IconButton
                      size="small"
                      color="success"
                      onClick={() => approveMut.mutate(row.id)}
                    >
                      <CheckCircleIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title={t('transactions.reject')}>
                    <IconButton
                      size="small"
                      color="error"
                      onClick={() => setRejectTarget(row)}
                    >
                      <CancelIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </>
              )}
              <Tooltip title={canEdit(row) ? t('common.edit') : t('transactions.approvedLocked')}>
                <span>
                  <IconButton
                    size="small"
                    disabled={!canEdit(row)}
                    onClick={() => {
                      setEditing(row);
                      setFormOpen(true);
                    }}
                  >
                    <EditIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
              {isManager && (
                <Tooltip title={t('common.delete')}>
                  <IconButton
                    size="small"
                    color="error"
                    onClick={() => setDeleteTarget(row)}
                  >
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
            </Stack>
          );
        },
      },
    ],
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [isManager, approveMut, branchNameById, t],
  );

  return (
    <Box>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        justifyContent="space-between"
        alignItems={{ sm: 'center' }}
        spacing={2}
        sx={{ mb: 3 }}
      >
        <Typography variant="h5">{t('transactions.title')}</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => {
            setEditing(null);
            setFormOpen(true);
          }}
        >
          {t('transactions.newTransaction')}
        </Button>
      </Stack>

      {!isManager && pettyCashBalance !== undefined && (
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={4}>
            <StatCard
              title={t('pettyCash.balanceLabel')}
              value={formatCurrency(pettyCashBalance)}
              icon={<PaidIcon />}
              color={pettyCashBalance > 0 ? 'info' : 'warning'}
            />
          </Grid>
        </Grid>
      )}

      {/* Filters */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label={t('common.type')}
                fullWidth
                size="small"
                value={filters.type ?? ''}
                onChange={(e) =>
                  updateFilter({ type: (e.target.value || undefined) as TransactionType })
                }
              >
                <MenuItem value="">{t('common.all')}</MenuItem>
                {TYPES.map((type) => (
                  <MenuItem key={type} value={type}>
                    {t(`enums.type.${type}`)}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label={t('common.status')}
                fullWidth
                size="small"
                value={filters.status ?? ''}
                onChange={(e) =>
                  updateFilter({ status: (e.target.value || undefined) as ApprovalStatus })
                }
              >
                <MenuItem value="">{t('common.all')}</MenuItem>
                {STATUSES.map((s) => (
                  <MenuItem key={s} value={s}>
                    {t(`enums.status.${s}`)}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label={t('common.branch')}
                fullWidth
                size="small"
                value={filters.branch ?? ''}
                onChange={(e) => updateFilter({ branch: e.target.value || undefined })}
              >
                <MenuItem value="">{t('common.all')}</MenuItem>
                {branches.map((b) => (
                  <MenuItem key={b.id} value={b.id}>
                    {b.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                label={t('common.category')}
                fullWidth
                size="small"
                value={filters.category ?? ''}
                onChange={(e) => updateFilter({ category: e.target.value || undefined })}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                label={t('common.from')}
                type="date"
                fullWidth
                size="small"
                InputLabelProps={{ shrink: true }}
                value={filters.from ?? ''}
                onChange={(e) => updateFilter({ from: e.target.value || undefined })}
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
                onChange={(e) => updateFilter({ to: e.target.value || undefined })}
              />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('transactions.failedToLoad'))}
        </Alert>
      )}

      <Card>
        <CardContent>
          <DataTable
            rows={data?.items ?? []}
            columns={columns}
            loading={isLoading || isFetching}
            getRowId={(row) => row.id}
            paginationMode="server"
            rowCount={data?.total ?? 0}
            paginationModel={paginationModel}
            onPaginationModelChange={(model) =>
              setFilters((prev) => ({
                ...prev,
                page: model.page + 1,
                pageSize: model.pageSize,
              }))
            }
          />
        </CardContent>
      </Card>

      <TransactionFormDialog
        open={formOpen}
        transaction={editing}
        onClose={() => setFormOpen(false)}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        title={t('transactions.deleteTitle')}
        message={t('transactions.deleteMessage', {
          type: deleteTarget ? t(`enums.type.${deleteTarget.type}`).toLowerCase() : '',
          amount: deleteTarget ? formatCurrency(deleteTarget.amount) : '',
        })}
        confirmLabel={t('common.delete')}
        confirmColor="error"
        loading={deleteMut.isPending}
        onConfirm={async () => {
          if (deleteTarget) await deleteMut.mutateAsync(deleteTarget.id);
          setDeleteTarget(null);
        }}
        onClose={() => setDeleteTarget(null)}
      />

      <ConfirmDialog
        open={!!rejectTarget}
        title={t('transactions.rejectTitle')}
        message={t('transactions.rejectMessage')}
        confirmLabel={t('transactions.reject')}
        confirmColor="error"
        loading={rejectMut.isPending}
        onConfirm={async () => {
          if (rejectTarget) await rejectMut.mutateAsync({ id: rejectTarget.id });
          setRejectTarget(null);
        }}
        onClose={() => setRejectTarget(null)}
      />
    </Box>
  );
}
