import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Controller, useForm, useWatch } from 'react-hook-form';
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
import Divider from '@mui/material/Divider';
import FormControlLabel from '@mui/material/FormControlLabel';
import Grid from '@mui/material/Grid';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import IconButton from '@mui/material/IconButton';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';

import type { GridColDef } from '@mui/x-data-grid';
import DataTable from '@/components/DataTable';
import { useBranches } from '@/hooks/useBranches';
import { useCars, useCreateCar, useUpdateCar } from '@/hooks/useCars';
import type { Car, CarInput } from '@/types';
import { formatCurrency, formatDate, getErrorMessage } from '@/utils/format';

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

function addMonthsIso(dateIso: string, months: number): string | null {
  if (!dateIso || !Number.isFinite(months)) return null;
  const d = new Date(dateIso);
  if (Number.isNaN(d.getTime())) return null;
  d.setMonth(d.getMonth() + months);
  return d.toISOString();
}

export default function Cars() {
  const { t } = useTranslation();
  const { data: cars = [], isLoading, isError, error } = useCars();
  const { data: branches = [] } = useBranches();
  const createMut = useCreateCar();
  const updateMut = useUpdateCar();

  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Car | null>(null);

  const branchNameById = useMemo(
    () => new Map(branches.map((b) => [b.id, b.name])),
    [branches],
  );

  const schema = useMemo(
    () =>
      z.object({
        branch: z.string().min(1, t('cars.branchRequired')),
        client: z.string().min(1, t('cars.clientRequired')),
        type: z.string().min(1, t('cars.typeRequired')),
        model: z.string().min(1, t('cars.modelRequired')),
        plateNumber: z.string().min(1, t('cars.plateRequired')),
        monthlyBill: z.coerce.number().min(0, t('cars.amountNonNegative')),
        initialDebt: z.coerce.number().min(0, t('cars.amountNonNegative')),
        contractStartDate: z.string().min(1, t('cars.contractStartRequired')),
        contractDurationMonths: z.coerce.number().int().positive(t('cars.durationPositive')),
        notes: z.string().optional(),
        isActive: z.boolean(),
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
    defaultValues: {
      branch: '',
      client: '',
      type: '',
      model: '',
      plateNumber: '',
      monthlyBill: 0,
      initialDebt: 0,
      contractStartDate: todayIso(),
      contractDurationMonths: 12,
      notes: '',
      isActive: true,
    },
  });

  useEffect(() => {
    if (!open) return;
    reset({
      branch: editing?.branch ?? branches[0]?.id ?? '',
      client: editing?.client ?? '',
      type: editing?.type ?? '',
      model: editing?.model ?? '',
      plateNumber: editing?.plateNumber ?? '',
      monthlyBill: editing?.monthlyBill ?? 0,
      initialDebt: editing?.initialDebt ?? 0,
      contractStartDate: editing ? editing.contractStartDate.slice(0, 10) : todayIso(),
      contractDurationMonths: editing?.contractDurationMonths ?? 12,
      notes: editing?.notes ?? '',
      isActive: editing?.isActive ?? true,
    });
  }, [open, editing, branches, reset]);

  const watchedStartDate = useWatch({ control, name: 'contractStartDate' });
  const watchedDuration = useWatch({ control, name: 'contractDurationMonths' });
  const contractEndPreview = useMemo(
    () => addMonthsIso(watchedStartDate, Number(watchedDuration)),
    [watchedStartDate, watchedDuration],
  );

  const onSubmit = async (values: FormValues) => {
    const payload: CarInput = {
      branch: values.branch,
      client: values.client,
      type: values.type,
      model: values.model,
      plateNumber: values.plateNumber,
      monthlyBill: values.monthlyBill,
      initialDebt: values.initialDebt,
      contractStartDate: values.contractStartDate,
      contractDurationMonths: values.contractDurationMonths,
      notes: values.notes || undefined,
      isActive: values.isActive,
    };
    if (editing) await updateMut.mutateAsync({ id: editing.id, payload });
    else await createMut.mutateAsync(payload);
    setOpen(false);
  };

  const columns: GridColDef<Car>[] = [
    { field: 'plateNumber', headerName: t('cars.plateNumber'), width: 130 },
    { field: 'model', headerName: t('cars.model'), flex: 1, minWidth: 140 },
    { field: 'client', headerName: t('cars.client'), flex: 1, minWidth: 140 },
    {
      field: 'branch',
      headerName: t('common.branch'),
      width: 130,
      valueFormatter: (value) => branchNameById.get(value as string) ?? (value as string),
    },
    {
      field: 'remainingDebt',
      headerName: t('cars.remainingDebt'),
      width: 150,
      valueFormatter: (value) => formatCurrency(value as number),
    },
    {
      field: 'contractEndDate',
      headerName: t('cars.contractEndDate'),
      width: 130,
      valueFormatter: (value) => formatDate(value as string),
    },
    {
      field: 'remainingBillingPeriods',
      headerName: t('cars.remainingBillingPeriods'),
      width: 150,
      valueGetter: (_value, row) => `${row.remainingBillingPeriods} / ${row.contractDurationMonths}`,
    },
    {
      field: 'isActive',
      headerName: t('common.status'),
      width: 100,
      valueFormatter: (value) => (value ? t('cars.active') : t('cars.inactive')),
    },
    {
      field: 'actions',
      headerName: '',
      width: 80,
      sortable: false,
      renderCell: (p) => (
        <IconButton
          size="small"
          onClick={() => {
            setEditing(p.row);
            setOpen(true);
          }}
        >
          <EditIcon fontSize="small" />
        </IconButton>
      ),
    },
  ];

  const serverError =
    (createMut.isError && getErrorMessage(createMut.error)) ||
    (updateMut.isError && getErrorMessage(updateMut.error)) ||
    null;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" sx={{ mb: 3 }}>
        <Typography variant="h5">{t('cars.title')}</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => {
            setEditing(null);
            setOpen(true);
          }}
        >
          {t('cars.newCar')}
        </Button>
      </Stack>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('cars.failedToLoad'))}
        </Alert>
      )}

      <Card>
        <CardContent>
          <DataTable
            rows={cars}
            columns={columns}
            loading={isLoading}
            getRowId={(row) => row.id}
          />
        </CardContent>
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editing ? t('cars.editCar') : t('cars.newCar')}</DialogTitle>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogContent dividers>
            {serverError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {serverError}
              </Alert>
            )}
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('cars.client')}
                  fullWidth
                  {...register('client')}
                  error={!!errors.client}
                  helperText={errors.client?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
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
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('cars.type')}
                  fullWidth
                  {...register('type')}
                  error={!!errors.type}
                  helperText={errors.type?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('cars.model')}
                  fullWidth
                  {...register('model')}
                  error={!!errors.model}
                  helperText={errors.model?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('cars.plateNumber')}
                  fullWidth
                  {...register('plateNumber')}
                  error={!!errors.plateNumber}
                  helperText={errors.plateNumber?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('cars.monthlyBill')}
                  type="number"
                  fullWidth
                  inputProps={{ step: '1', min: '0' }}
                  {...register('monthlyBill')}
                  error={!!errors.monthlyBill}
                  helperText={errors.monthlyBill?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('cars.initialDebt')}
                  type="number"
                  fullWidth
                  inputProps={{ step: '1', min: '0' }}
                  {...register('initialDebt')}
                  error={!!errors.initialDebt}
                  helperText={errors.initialDebt?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('cars.contractStartDate')}
                  type="date"
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  {...register('contractStartDate')}
                  error={!!errors.contractStartDate}
                  helperText={errors.contractStartDate?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('cars.contractDurationMonths')}
                  type="number"
                  fullWidth
                  inputProps={{ step: '1', min: '1' }}
                  {...register('contractDurationMonths')}
                  error={!!errors.contractDurationMonths}
                  helperText={
                    errors.contractDurationMonths?.message ?? t('cars.durationIsBillingPeriods')
                  }
                />
              </Grid>

              <Grid item xs={12}>
                <Divider sx={{ my: 1 }} />
                <Stack spacing={0.5}>
                  <Typography variant="body2" color="text.secondary">
                    {t('cars.contractEndDate')}:{' '}
                    <b>{contractEndPreview ? formatDate(contractEndPreview) : '—'}</b>
                  </Typography>
                  {editing && (
                    <>
                      <Typography variant="body2" color="text.secondary">
                        {t('cars.remainingDebt')}: <b>{formatCurrency(editing.remainingDebt)}</b>
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {t('cars.remainingBillingPeriods')}:{' '}
                        <b>
                          {editing.remainingBillingPeriods} / {editing.contractDurationMonths}
                        </b>{' '}
                        ({t('cars.billingPeriodsPaid', { count: editing.paidBillingPeriods })})
                      </Typography>
                    </>
                  )}
                </Stack>
              </Grid>

              <Grid item xs={12}>
                <TextField
                  label={t('cars.notes')}
                  fullWidth
                  multiline
                  minRows={2}
                  {...register('notes')}
                  error={!!errors.notes}
                  helperText={errors.notes?.message}
                />
              </Grid>
              {editing && (
                <Grid item xs={12}>
                  <Controller
                    control={control}
                    name="isActive"
                    render={({ field }) => (
                      <FormControlLabel
                        control={
                          <Switch checked={field.value} onChange={(e) => field.onChange(e.target.checked)} />
                        }
                        label={field.value ? t('cars.active') : t('cars.inactive')}
                      />
                    )}
                  />
                </Grid>
              )}
            </Grid>
          </DialogContent>
          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button onClick={() => setOpen(false)} disabled={isSubmitting}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isSubmitting ? t('common.saving') : t('common.save')}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
}
