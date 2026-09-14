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
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Divider from '@mui/material/Divider';
import GlobalStyles from '@mui/material/GlobalStyles';
import Grid from '@mui/material/Grid';
import IconButton from '@mui/material/IconButton';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';

import AddIcon from '@mui/icons-material/Add';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import DeleteIcon from '@mui/icons-material/Delete';
import PrintIcon from '@mui/icons-material/Print';
import UndoIcon from '@mui/icons-material/Undo';

import type { GridColDef } from '@mui/x-data-grid';
import DataTable from '@/components/DataTable';
import ConfirmDialog from '@/components/ConfirmDialog';
import { useBranches } from '@/hooks/useBranches';
import { useCars } from '@/hooks/useCars';
import {
  useCreateInvoice,
  useDeleteInvoice,
  useInvoices,
  useMarkInvoicePaid,
  useVoidInvoice,
} from '@/hooks/useInvoices';
import type { CreateInvoiceInput, Invoice, InvoiceStatus, InvoiceType, TaxScheme } from '@/types';
import { formatCurrency, formatDate, getErrorMessage } from '@/utils/format';

const PPN_RATE = 0.11;
const PPH23_RATE = 0.02;

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

function taxBases(scheme: TaxScheme, wage: number, fee: number): [number, number] {
  if (scheme === 'WageOnly') return [wage, wage];
  if (scheme === 'FeeOnly') return [fee, fee];
  return [wage + fee, fee]; // Combined
}

export default function Invoices() {
  const { t } = useTranslation();
  const { data: branches = [] } = useBranches();
  const { data: cars = [] } = useCars();
  const [statusTab, setStatusTab] = useState<'All' | InvoiceStatus>('All');

  const { data, isLoading, isError, error } = useInvoices({
    status: statusTab === 'All' ? undefined : statusTab,
    pageSize: 100,
  });
  const createMut = useCreateInvoice();
  const markPaidMut = useMarkInvoicePaid();
  const deleteMut = useDeleteInvoice();
  const voidMut = useVoidInvoice();

  const [open, setOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Invoice | null>(null);
  const [payTarget, setPayTarget] = useState<Invoice | null>(null);
  const [voidTarget, setVoidTarget] = useState<Invoice | null>(null);
  const [printTarget, setPrintTarget] = useState<Invoice | null>(null);

  const branchNameById = useMemo(() => new Map(branches.map((b) => [b.id, b.name])), [branches]);
  const activeCars = useMemo(() => cars.filter((c) => c.isActive), [cars]);
  const carById = useMemo(() => new Map(cars.map((c) => [c.id, c])), [cars]);

  const schema = useMemo(
    () =>
      z
        .object({
          type: z.enum(['Rental', 'ServiceBill']),
          branch: z.string().min(1, t('invoices.branchRequired')),
          invoiceDate: z.string().min(1, t('invoices.dateRequired')),
          carId: z.string().optional(),
          clientName: z.string().optional(),
          driverName: z.string().optional(),
          wageDeposit: z.coerce.number().min(0).optional(),
          fee: z.coerce.number().min(0).optional(),
          taxScheme: z.enum(['Combined', 'WageOnly', 'FeeOnly']).optional(),
        })
        .refine((v) => v.type !== 'Rental' || !!v.carId, {
          message: t('invoices.carRequired'),
          path: ['carId'],
        })
        .refine((v) => v.type !== 'ServiceBill' || !!v.clientName, {
          message: t('invoices.clientRequired'),
          path: ['clientName'],
        })
        .refine((v) => v.type !== 'ServiceBill' || v.wageDeposit !== undefined, {
          message: t('invoices.wageRequired'),
          path: ['wageDeposit'],
        })
        .refine((v) => v.type !== 'ServiceBill' || v.fee !== undefined, {
          message: t('invoices.feeRequired'),
          path: ['fee'],
        })
        .refine((v) => v.type !== 'ServiceBill' || !!v.taxScheme, {
          message: t('invoices.schemeRequired'),
          path: ['taxScheme'],
        }),
    [t],
  );
  type FormValues = z.infer<typeof schema>;

  const {
    control,
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      type: 'Rental',
      branch: '',
      invoiceDate: todayIso(),
      carId: '',
      clientName: '',
      driverName: '',
      wageDeposit: 0,
      fee: 0,
      taxScheme: 'Combined',
    },
  });

  const watchedType: InvoiceType = useWatch({ control, name: 'type' });
  const watchedCarId = useWatch({ control, name: 'carId' });
  const watchedWage = useWatch({ control, name: 'wageDeposit' }) ?? 0;
  const watchedFee = useWatch({ control, name: 'fee' }) ?? 0;
  const watchedScheme = useWatch({ control, name: 'taxScheme' }) ?? 'Combined';

  useEffect(() => {
    if (!open) return;
    reset({
      type: 'Rental',
      branch: '',
      invoiceDate: todayIso(),
      carId: '',
      clientName: '',
      driverName: '',
      wageDeposit: 0,
      fee: 0,
      taxScheme: 'Combined',
    });
  }, [open, reset]);

  // Rental invoices derive their branch from the selected car.
  useEffect(() => {
    if (watchedType === 'Rental' && watchedCarId) {
      const car = carById.get(watchedCarId);
      if (car) setValue('branch', car.branch);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [watchedType, watchedCarId]);

  const preview = useMemo(() => {
    if (watchedType === 'Rental') {
      const car = watchedCarId ? carById.get(watchedCarId) : undefined;
      const base = car?.monthlyBill ?? 0;
      const ppn = Math.round(base * PPN_RATE);
      return { base, ppn, pph23: 0, total: base + ppn, client: car?.client ?? '' };
    }
    const [ppnBase, pph23Base] = taxBases(watchedScheme as TaxScheme, watchedWage, watchedFee);
    const ppn = Math.round(ppnBase * PPN_RATE);
    const pph23 = Math.round(pph23Base * PPH23_RATE);
    return {
      base: watchedWage + watchedFee,
      ppn,
      pph23,
      total: watchedWage + watchedFee + ppn - pph23,
      client: '',
    };
  }, [watchedType, watchedCarId, watchedWage, watchedFee, watchedScheme, carById]);

  const onSubmit = async (values: FormValues) => {
    const payload: CreateInvoiceInput = {
      type: values.type,
      branch: values.branch,
      invoiceDate: values.invoiceDate,
      carId: values.type === 'Rental' ? values.carId : undefined,
      clientName: values.type === 'ServiceBill' ? values.clientName : undefined,
      driverName: values.type === 'ServiceBill' ? values.driverName || undefined : undefined,
      wageDeposit: values.type === 'ServiceBill' ? values.wageDeposit : undefined,
      fee: values.type === 'ServiceBill' ? values.fee : undefined,
      taxScheme: values.type === 'ServiceBill' ? values.taxScheme : undefined,
    };
    await createMut.mutateAsync(payload);
    setOpen(false);
  };

  const columns: GridColDef<Invoice>[] = [
    {
      field: 'invoiceDate',
      headerName: t('common.date'),
      width: 120,
      valueFormatter: (value) => formatDate(value as string),
    },
    {
      field: 'type',
      headerName: t('common.type'),
      width: 130,
      renderCell: (p) => (
        <Chip
          size="small"
          label={t(`invoices.type.${p.row.type}`)}
          color={p.row.type === 'Rental' ? 'primary' : 'secondary'}
          variant="outlined"
        />
      ),
    },
    { field: 'clientName', headerName: t('invoices.client'), flex: 1, minWidth: 150 },
    {
      field: 'branch',
      headerName: t('common.branch'),
      width: 120,
      valueFormatter: (value) => branchNameById.get(value as string) ?? (value as string),
    },
    {
      field: 'totalAmount',
      headerName: t('invoices.total'),
      width: 150,
      valueFormatter: (value) => formatCurrency(value as number),
    },
    {
      field: 'status',
      headerName: t('common.status'),
      width: 110,
      renderCell: (p) => (
        <Chip
          size="small"
          label={t(`invoices.status.${p.row.status}`)}
          color={p.row.status === 'Paid' ? 'success' : 'warning'}
        />
      ),
    },
    {
      field: 'actions',
      headerName: t('common.actions'),
      width: 150,
      sortable: false,
      filterable: false,
      renderCell: (p) => {
        const row = p.row;
        return (
          <Stack direction="row" spacing={0.5}>
            {row.status === 'Unpaid' && (
              <Tooltip title={t('invoices.markPaid')}>
                <IconButton size="small" color="success" onClick={() => setPayTarget(row)}>
                  <CheckCircleIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
            <Tooltip title={t('invoices.print')}>
              <IconButton size="small" onClick={() => setPrintTarget(row)}>
                <PrintIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            {row.status === 'Unpaid' && (
              <Tooltip title={t('common.delete')}>
                <IconButton size="small" color="error" onClick={() => setDeleteTarget(row)}>
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
            {row.status === 'Paid' && (
              <Tooltip title={t('invoices.void')}>
                <IconButton size="small" color="warning" onClick={() => setVoidTarget(row)}>
                  <UndoIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
          </Stack>
        );
      },
    },
  ];

  const serverError = createMut.isError ? getErrorMessage(createMut.error) : null;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">{t('invoices.title')}</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          {t('invoices.newInvoice')}
        </Button>
      </Stack>

      <Tabs
        value={statusTab}
        onChange={(_, v) => setStatusTab(v)}
        sx={{ mb: 2 }}
      >
        <Tab value="All" label={t('invoices.all')} />
        <Tab value="Unpaid" label={t('invoices.accountsReceivable')} />
        <Tab value="Paid" label={t('invoices.paid')} />
      </Tabs>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('invoices.failedToLoad'))}
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

      {/* New invoice dialog */}
      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{t('invoices.newInvoice')}</DialogTitle>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogContent dividers>
            {serverError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {serverError}
              </Alert>
            )}
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <Controller
                  control={control}
                  name="type"
                  render={({ field }) => (
                    <TextField {...field} select label={t('invoices.invoiceType')} fullWidth>
                      <MenuItem value="Rental">{t('invoices.type.Rental')}</MenuItem>
                      <MenuItem value="ServiceBill">{t('invoices.type.ServiceBill')}</MenuItem>
                    </TextField>
                  )}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('invoices.invoiceDate')}
                  type="date"
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  {...register('invoiceDate')}
                  error={!!errors.invoiceDate}
                  helperText={errors.invoiceDate?.message}
                />
              </Grid>

              {watchedType === 'Rental' ? (
                <Grid item xs={12}>
                  <Controller
                    control={control}
                    name="carId"
                    render={({ field }) => (
                      <TextField
                        {...field}
                        select
                        label={t('invoices.car')}
                        fullWidth
                        error={!!errors.carId}
                        helperText={errors.carId?.message}
                      >
                        {activeCars.map((c) => (
                          <MenuItem key={c.id} value={c.id}>
                            {c.plateNumber} — {c.model} ({c.client})
                          </MenuItem>
                        ))}
                      </TextField>
                    )}
                  />
                </Grid>
              ) : (
                <>
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
                      label={t('invoices.client')}
                      fullWidth
                      {...register('clientName')}
                      error={!!errors.clientName}
                      helperText={errors.clientName?.message}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      label={t('invoices.driver')}
                      fullWidth
                      {...register('driverName')}
                      error={!!errors.driverName}
                      helperText={errors.driverName?.message}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      control={control}
                      name="taxScheme"
                      render={({ field }) => (
                        <TextField
                          {...field}
                          select
                          label={t('invoices.taxScheme')}
                          fullWidth
                          error={!!errors.taxScheme}
                          helperText={errors.taxScheme?.message}
                        >
                          <MenuItem value="Combined">{t('invoices.scheme.Combined')}</MenuItem>
                          <MenuItem value="WageOnly">{t('invoices.scheme.WageOnly')}</MenuItem>
                          <MenuItem value="FeeOnly">{t('invoices.scheme.FeeOnly')}</MenuItem>
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      label={t('invoices.wageDeposit')}
                      type="number"
                      fullWidth
                      inputProps={{ step: '1', min: '0' }}
                      {...register('wageDeposit')}
                      error={!!errors.wageDeposit}
                      helperText={errors.wageDeposit?.message}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      label={t('invoices.fee')}
                      type="number"
                      fullWidth
                      inputProps={{ step: '1', min: '0' }}
                      {...register('fee')}
                      error={!!errors.fee}
                      helperText={errors.fee?.message}
                    />
                  </Grid>
                </>
              )}

              <Grid item xs={12}>
                <Divider sx={{ my: 1 }} />
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  {t('invoices.preview')}
                </Typography>
                <Stack spacing={0.5}>
                  {watchedType === 'Rental' && preview.client && (
                    <Typography variant="body2" color="text.secondary">
                      {t('invoices.client')}: {preview.client}
                    </Typography>
                  )}
                  <Typography variant="body2" color="text.secondary">
                    PPN (11%): {formatCurrency(preview.ppn)}
                  </Typography>
                  {watchedType === 'ServiceBill' && (
                    <Typography variant="body2" color="text.secondary">
                      PPH23 (2%): -{formatCurrency(preview.pph23)}
                    </Typography>
                  )}
                  <Typography variant="subtitle1" fontWeight={700}>
                    {t('invoices.total')}: {formatCurrency(preview.total)}
                  </Typography>
                </Stack>
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button onClick={() => setOpen(false)} disabled={isSubmitting}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isSubmitting ? t('common.saving') : t('common.create')}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Mark paid confirmation */}
      <ConfirmDialog
        open={!!payTarget}
        title={t('invoices.markPaidTitle')}
        message={t('invoices.markPaidMessage', {
          amount: payTarget ? formatCurrency(payTarget.totalAmount) : '',
        })}
        confirmLabel={t('invoices.markPaid')}
        confirmColor="success"
        loading={markPaidMut.isPending}
        onConfirm={async () => {
          if (payTarget) await markPaidMut.mutateAsync(payTarget.id);
          setPayTarget(null);
        }}
        onClose={() => setPayTarget(null)}
      />

      {/* Delete confirmation */}
      <ConfirmDialog
        open={!!deleteTarget}
        title={t('invoices.deleteTitle')}
        message={t('invoices.deleteMessage', {
          amount: deleteTarget ? formatCurrency(deleteTarget.totalAmount) : '',
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

      {/* Void confirmation */}
      <ConfirmDialog
        open={!!voidTarget}
        title={t('invoices.voidTitle')}
        message={t('invoices.voidMessage', {
          amount: voidTarget ? formatCurrency(voidTarget.totalAmount) : '',
        })}
        confirmLabel={t('invoices.void')}
        confirmColor="warning"
        loading={voidMut.isPending}
        onConfirm={async () => {
          if (voidTarget) await voidMut.mutateAsync(voidTarget.id);
          setVoidTarget(null);
        }}
        onClose={() => setVoidTarget(null)}
      />

      {/* Print view */}
      <InvoicePrintDialog invoice={printTarget} onClose={() => setPrintTarget(null)} />
    </Box>
  );
}

function InvoicePrintDialog({ invoice, onClose }: { invoice: Invoice | null; onClose: () => void }) {
  const { t } = useTranslation();
  if (!invoice) return null;

  const isRental = invoice.type === 'Rental';

  return (
    <Dialog open={!!invoice} onClose={onClose} maxWidth="sm" fullWidth>
      <GlobalStyles
        styles={{
          '@media print': {
            'body *': { visibility: 'hidden' },
            '#invoice-print-area, #invoice-print-area *': { visibility: 'visible' },
            '#invoice-print-area': {
              position: 'fixed',
              top: 0,
              left: 0,
              width: '100%',
              padding: '24px',
            },
          },
        }}
      />
      <DialogContent id="invoice-print-area">
        <Stack spacing={2}>
          <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
            <Box>
              <Typography variant="h6" fontWeight={700}>
                {t(`invoices.type.${invoice.type}`)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {formatDate(invoice.invoiceDate)}
              </Typography>
            </Box>
            <Chip
              label={t(`invoices.status.${invoice.status}`)}
              color={invoice.status === 'Paid' ? 'success' : 'warning'}
            />
          </Stack>

          <Divider />

          <Stack spacing={0.5}>
            <Typography variant="body2">
              <b>{t('invoices.client')}:</b> {invoice.clientName}
            </Typography>
            {invoice.driverName && (
              <Typography variant="body2">
                <b>{t('invoices.driver')}:</b> {invoice.driverName}
              </Typography>
            )}
          </Stack>

          <Divider />

          <Stack spacing={0.5}>
            {isRental ? (
              <Row label={t('invoices.monthlyBill')} value={formatCurrency(invoice.monthlyBill ?? 0)} />
            ) : (
              <>
                <Row label={t('invoices.wageDeposit')} value={formatCurrency(invoice.wageDeposit ?? 0)} />
                <Row label={t('invoices.fee')} value={formatCurrency(invoice.fee ?? 0)} />
              </>
            )}
            <Row label="PPN (11%)" value={formatCurrency(invoice.ppnAmount)} />
            {invoice.pph23Amount > 0 && (
              <Row label="PPH23 (2%)" value={`-${formatCurrency(invoice.pph23Amount)}`} />
            )}
            <Divider sx={{ my: 1 }} />
            <Row label={t('invoices.total')} value={formatCurrency(invoice.totalAmount)} bold />
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.close')}</Button>
        <Button variant="contained" startIcon={<PrintIcon />} onClick={() => window.print()}>
          {t('invoices.print')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function Row({ label, value, bold }: { label: string; value: string; bold?: boolean }) {
  return (
    <Stack direction="row" justifyContent="space-between">
      <Typography variant={bold ? 'subtitle1' : 'body2'} fontWeight={bold ? 700 : 400}>
        {label}
      </Typography>
      <Typography variant={bold ? 'subtitle1' : 'body2'} fontWeight={bold ? 700 : 400}>
        {value}
      </Typography>
    </Stack>
  );
}
