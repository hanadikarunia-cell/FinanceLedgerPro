import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Grid from '@mui/material/Grid';
import IconButton from '@mui/material/IconButton';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import AddIcon from '@mui/icons-material/Add';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import DeleteIcon from '@mui/icons-material/Delete';

import type { GridColDef } from '@mui/x-data-grid';
import DataTable from '@/components/DataTable';
import ConfirmDialog from '@/components/ConfirmDialog';
import { StatusChip } from '@/components/StatusChip';
import { useBranches } from '@/hooks/useBranches';
import {
  useApprovePettyCashRequest,
  useCreatePettyCashRequest,
  useDeletePettyCashRequest,
  usePettyCashRequests,
  useRejectPettyCashRequest,
} from '@/hooks/usePettyCashRequests';
import { useAuth } from '@/context/AuthContext';
import type { PettyCashRequest, PettyCashRequestInput } from '@/types';
import { formatCurrency, formatDate, getErrorMessage } from '@/utils/format';

export default function PettyCashRequests() {
  const { t } = useTranslation();
  const { isManager } = useAuth();
  const { data: branches = [] } = useBranches();

  const { data, isLoading, isError, error } = usePettyCashRequests({ page: 1, pageSize: 50 });
  const createMut = useCreatePettyCashRequest();
  const approveMut = useApprovePettyCashRequest();
  const rejectMut = useRejectPettyCashRequest();
  const deleteMut = useDeletePettyCashRequest();

  const [open, setOpen] = useState(false);
  const [rejectTarget, setRejectTarget] = useState<PettyCashRequest | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<PettyCashRequest | null>(null);

  const schema = useMemo(
    () =>
      z.object({
        amount: z.coerce.number().positive(t('pettyCash.amountPositive')),
        reason: z.string().min(1, t('pettyCash.reasonRequired')).max(500),
        branch: z.string().min(1, t('pettyCash.branchRequired')),
      }),
    [t],
  );
  type FormValues = z.infer<typeof schema>;

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { amount: 0, reason: '', branch: '' },
  });

  const onOpen = () => {
    reset({ amount: 0, reason: '', branch: branches[0]?.id ?? '' });
    setOpen(true);
  };

  const onSubmit = async (values: FormValues) => {
    const payload: PettyCashRequestInput = values;
    await createMut.mutateAsync(payload);
    setOpen(false);
  };

  const columns: GridColDef<PettyCashRequest>[] = [
    {
      field: 'requestedDate',
      headerName: t('pettyCash.requestedDate'),
      width: 160,
      valueFormatter: (value) => formatDate(value as string),
    },
    { field: 'requestedByName', headerName: t('pettyCash.requestedBy'), width: 160 },
    {
      field: 'amount',
      headerName: t('common.amount'),
      width: 140,
      valueFormatter: (value) => formatCurrency(value as number),
    },
    { field: 'reason', headerName: t('pettyCash.reason'), flex: 1, minWidth: 200 },
    {
      field: 'status',
      headerName: t('common.status'),
      width: 130,
      renderCell: (params) => <StatusChip status={params.row.status} />,
    },
    {
      field: 'actions',
      headerName: t('common.actions'),
      width: 180,
      sortable: false,
      filterable: false,
      renderCell: (params) => {
        const row = params.row;
        if (!isManager) return null;
        return (
          <Stack direction="row" spacing={0.5}>
            {row.status === 'Submitted' && (
              <>
                <Tooltip title={t('pettyCash.approve')}>
                  <IconButton
                    size="small"
                    color="success"
                    onClick={() => approveMut.mutate(row.id)}
                  >
                    <CheckCircleIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
                <Tooltip title={t('pettyCash.reject')}>
                  <IconButton size="small" color="error" onClick={() => setRejectTarget(row)}>
                    <CancelIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              </>
            )}
            {row.status !== 'Approved' && (
              <Tooltip title={t('common.delete')}>
                <IconButton size="small" color="error" onClick={() => setDeleteTarget(row)}>
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
          </Stack>
        );
      },
    },
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" sx={{ mb: 3 }}>
        <Typography variant="h5">{t('pettyCash.title')}</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={onOpen}>
          {t('pettyCash.newRequest')}
        </Button>
      </Stack>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('pettyCash.failedToLoad'))}
        </Alert>
      )}

      <Card>
        <CardContent>
          <DataTable
            rows={data?.items ?? []}
            columns={columns}
            loading={isLoading}
            getRowId={(row) => row.id}
          />
        </CardContent>
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{t('pettyCash.newRequest')}</DialogTitle>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogContent dividers>
            {createMut.isError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {getErrorMessage(createMut.error)}
              </Alert>
            )}
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField
                  label={t('common.amount')}
                  type="number"
                  fullWidth
                  inputProps={{ step: '1', min: '0' }}
                  {...register('amount')}
                  error={!!errors.amount}
                  helperText={errors.amount?.message}
                />
              </Grid>
              <Grid item xs={12}>
                <Controller
                  control={control}
                  name="branch"
                  render={({ field }) => (
                    <TextField
                      {...field}
                      select
                      label={t('common.branch')}
                      fullWidth
                      error={!!errors.branch}
                      helperText={errors.branch?.message}
                    >
                      {branches.map((b) => (
                        <MenuItem key={b.id} value={b.id}>
                          {b.name}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label={t('pettyCash.reason')}
                  fullWidth
                  multiline
                  minRows={2}
                  {...register('reason')}
                  error={!!errors.reason}
                  helperText={errors.reason?.message}
                />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button onClick={() => setOpen(false)} disabled={isSubmitting}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isSubmitting ? t('common.saving') : t('pettyCash.newRequest')}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      <ConfirmDialog
        open={!!rejectTarget}
        title={t('pettyCash.rejectTitle')}
        message={t('pettyCash.rejectMessage')}
        confirmLabel={t('pettyCash.reject')}
        confirmColor="error"
        loading={rejectMut.isPending}
        onConfirm={async () => {
          if (rejectTarget) await rejectMut.mutateAsync({ id: rejectTarget.id });
          setRejectTarget(null);
        }}
        onClose={() => setRejectTarget(null)}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        title={t('pettyCash.deleteTitle')}
        message={t('pettyCash.deleteMessage', {
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
    </Box>
  );
}
