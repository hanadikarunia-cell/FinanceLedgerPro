import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Grid from '@mui/material/Grid';
import InputAdornment from '@mui/material/InputAdornment';
import MenuItem from '@mui/material/MenuItem';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import FileUpload from '@/components/FileUpload';
import { useBranches } from '@/hooks/useBranches';
import { useCreateTransaction, useUpdateTransaction, useUsersLookup } from '@/hooks/useTransactions';
import { useCarsLookup } from '@/hooks/useCars';
import { useAuth } from '@/context/AuthContext';
import { usePettyCashBalance } from '@/hooks/usePettyCashBalance';
import type { Attachment, Transaction, TransactionInput, TransactionType } from '@/types';
import { formatCurrency, getErrorMessage } from '@/utils/format';

const INCOME_CATEGORIES = ['Rent', 'Interest', 'Invoice', 'Salaries', 'Other'];

const EXPENSE_CATEGORIES = [
  'Service',
  'Salaries',
  'Entertainment',
  'Office Utilities',
  'Taxes - PPN',
  'Taxes - PPH21',
  'Taxes - PPH25',
  'Taxes - PPH23',
  'Taxes - Other',
  'Car Debt',
  'Other',
];

interface Props {
  open: boolean;
  transaction: Transaction | null; // null = create mode
  onClose: () => void;
}

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

export default function TransactionFormDialog({ open, transaction, onClose }: Props) {
  const { t } = useTranslation();
  const { isManager } = useAuth();
  const isEdit = !!transaction;
  const { data: branches = [] } = useBranches();
  const { data: usersLookup = [] } = useUsersLookup();
  const { data: carsLookup = [] } = useCarsLookup();
  const { data: pettyCashBalance } = usePettyCashBalance();
  const createMut = useCreateTransaction();
  const updateMut = useUpdateTransaction();

  const [attachments, setAttachments] = useState<Attachment[]>([]);

  const schema = useMemo(
    () =>
      z
        .object({
          type: z.enum(['Income', 'Expense']),
          category: z.string().min(1, t('transactionForm.categoryRequired')).max(80),
          amount: z.coerce.number().positive(t('transactionForm.amountPositive')),
          date: z.string().min(1, t('transactionForm.dateRequired')),
          branch: z.string().min(1, t('transactionForm.branchRequired')),
          description: z.string().max(500).optional(),
          relatedUserId: z.string().optional(),
          carId: z.string().optional(),
        })
        .refine(
          (v) => !(v.type === 'Expense' && v.category === 'Salaries') || !!v.relatedUserId,
          { message: t('transactionForm.relatedUserRequired'), path: ['relatedUserId'] },
        )
        .refine(
          (v) =>
            !(v.type === 'Expense' && ['Service', 'Car Debt'].includes(v.category)) || !!v.carId,
          { message: t('transactionForm.relatedCarRequired'), path: ['carId'] },
        ),
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
      type: 'Expense',
      category: '',
      amount: 0,
      date: todayIso(),
      branch: '',
      description: '',
      relatedUserId: '',
      carId: '',
    },
  });

  const watchedType: TransactionType = useWatch({ control, name: 'type' });
  const watchedCategory = useWatch({ control, name: 'category' });
  const categories = watchedType === 'Income' ? INCOME_CATEGORIES : EXPENSE_CATEGORIES;
  const showRelatedUser = watchedType === 'Expense' && watchedCategory === 'Salaries';
  const showRelatedCar =
    watchedType === 'Expense' && ['Service', 'Car Debt'].includes(watchedCategory);
  const showBalanceHint = !isManager && watchedType === 'Expense' && watchedCategory !== 'Petty Cash';

  useEffect(() => {
    if (!open) return;
    reset(
      transaction
        ? {
            type: transaction.type,
            category: transaction.category,
            amount: transaction.amount,
            date: transaction.date.slice(0, 10),
            branch: transaction.branch,
            description: transaction.description ?? '',
            relatedUserId: transaction.relatedUserId ?? '',
            carId: transaction.carId ?? '',
          }
        : {
            type: 'Expense',
            category: '',
            amount: 0,
            date: todayIso(),
            branch: branches[0]?.id ?? '',
            description: '',
            relatedUserId: '',
            carId: '',
          },
    );
    // The API only returns attachment IDs on a transaction, not full file
    // metadata, so previously-attached files can't be shown here when editing.
    setAttachments([]);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, transaction, branches.length]);

  // Reset category when switching Type if it's no longer a valid option.
  useEffect(() => {
    if (watchedCategory && !categories.includes(watchedCategory)) {
      setValue('category', '');
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [watchedType]);

  const serverError =
    (createMut.isError && getErrorMessage(createMut.error)) ||
    (updateMut.isError && getErrorMessage(updateMut.error)) ||
    null;

  const onSubmit = async (values: FormValues) => {
    const payload: TransactionInput = {
      ...values,
      description: values.description || undefined,
      relatedUserId: values.relatedUserId || undefined,
      carId: values.carId || undefined,
      attachmentIds: attachments.map((a) => a.id),
    };
    if (isEdit && transaction) {
      await updateMut.mutateAsync({ id: transaction.id, payload });
    } else {
      await createMut.mutateAsync(payload);
    }
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        {isEdit ? t('transactionForm.editTransaction') : t('transactionForm.newTransaction')}
      </DialogTitle>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <DialogContent dividers>
          {serverError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {serverError}
            </Alert>
          )}
          {showBalanceHint && pettyCashBalance !== undefined && (
            <Alert severity="info" sx={{ mb: 2 }}>
              {t('pettyCash.currentBalance', { amount: formatCurrency(pettyCashBalance) })}
            </Alert>
          )}
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <Controller
                control={control}
                name="type"
                render={({ field }) => (
                  <TextField
                    {...field}
                    select
                    label={t('common.type')}
                    fullWidth
                    error={!!errors.type}
                    helperText={errors.type?.message}
                  >
                    <MenuItem value="Income">{t('transactionForm.income')}</MenuItem>
                    <MenuItem value="Expense">{t('transactionForm.expense')}</MenuItem>
                  </TextField>
                )}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Controller
                control={control}
                name="category"
                render={({ field }) => (
                  <TextField
                    {...field}
                    select
                    label={t('common.category')}
                    fullWidth
                    error={!!errors.category}
                    helperText={errors.category?.message}
                  >
                    {categories.map((c) => (
                      <MenuItem key={c} value={c}>
                        {t(`enums.category.${c}`)}
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                label={t('common.amount')}
                type="number"
                fullWidth
                inputProps={{ step: '1', min: '0' }}
                InputProps={{
                  startAdornment: <InputAdornment position="start">Rp</InputAdornment>,
                }}
                {...register('amount')}
                error={!!errors.amount}
                helperText={errors.amount?.message}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                label={t('common.date')}
                type="date"
                fullWidth
                InputLabelProps={{ shrink: true }}
                {...register('date')}
                error={!!errors.date}
                helperText={errors.date?.message}
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
                    {branches.length === 0 && (
                      <MenuItem value="" disabled>
                        {t('transactionForm.noBranchesAvailable')}
                      </MenuItem>
                    )}
                    {branches.map((b) => (
                      <MenuItem key={b.id} value={b.id}>
                        {b.name}
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />
            </Grid>
            {showRelatedUser && (
              <Grid item xs={12}>
                <Controller
                  control={control}
                  name="relatedUserId"
                  render={({ field }) => (
                    <TextField
                      {...field}
                      select
                      label={t('transactionForm.relatedUser')}
                      fullWidth
                      error={!!errors.relatedUserId}
                      helperText={errors.relatedUserId?.message}
                    >
                      {usersLookup.map((u) => (
                        <MenuItem key={u.id} value={u.id}>
                          {u.displayName}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
            )}
            {showRelatedCar && (
              <Grid item xs={12}>
                <Controller
                  control={control}
                  name="carId"
                  render={({ field }) => (
                    <TextField
                      {...field}
                      select
                      label={t('transactionForm.relatedCar')}
                      fullWidth
                      error={!!errors.carId}
                      helperText={errors.carId?.message}
                    >
                      {carsLookup.map((c) => (
                        <MenuItem key={c.id} value={c.id}>
                          {c.label}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
            )}
            <Grid item xs={12}>
              <TextField
                label={t('transactionForm.description')}
                fullWidth
                multiline
                minRows={2}
                {...register('description')}
                error={!!errors.description}
                helperText={errors.description?.message}
              />
            </Grid>
            <Grid item xs={12}>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>
                {t('transactionForm.attachments')}
              </Typography>
              <FileUpload attachments={attachments} onChange={setAttachments} />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button onClick={onClose} disabled={isSubmitting}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {isSubmitting
              ? t('common.saving')
              : isEdit
                ? t('transactionForm.saveChanges')
                : t('common.create')}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
