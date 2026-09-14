import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
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
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import AddIcon from '@mui/icons-material/Add';

import type { GridColDef } from '@mui/x-data-grid';
import DataTable from '@/components/DataTable';
import { useBranches, useCreateBranch } from '@/hooks/useBranches';
import type { Branch, BranchInput } from '@/types';
import { getErrorMessage } from '@/utils/format';

export default function Branches() {
  const { t } = useTranslation();
  const { data: branches = [], isLoading, isError, error } = useBranches();
  const createMut = useCreateBranch();
  const [open, setOpen] = useState(false);

  const schema = useMemo(
    () =>
      z.object({
        name: z.string().min(1, t('branches.nameRequired')),
        code: z.string().min(1, t('branches.codeRequired')).max(20),
        address: z.string().optional(),
      }),
    [t],
  );
  type FormValues = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', code: '', address: '' },
  });

  useEffect(() => {
    if (open) reset({ name: '', code: '', address: '' });
  }, [open, reset]);

  const onSubmit = async (values: FormValues) => {
    const payload: BranchInput = {
      name: values.name,
      code: values.code,
      address: values.address || undefined,
    };
    await createMut.mutateAsync(payload);
    setOpen(false);
  };

  const columns: GridColDef<Branch>[] = [
    { field: 'name', headerName: t('common.name'), flex: 1, minWidth: 180 },
    { field: 'code', headerName: t('branches.code'), width: 140 },
    { field: 'address', headerName: t('branches.address'), flex: 1, minWidth: 200 },
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" sx={{ mb: 3 }}>
        <Typography variant="h5">{t('branches.title')}</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          {t('branches.newBranch')}
        </Button>
      </Stack>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('branches.failedToLoad'))}
        </Alert>
      )}

      <Card>
        <CardContent>
          <DataTable
            rows={branches}
            columns={columns}
            loading={isLoading}
            getRowId={(row) => row.id}
          />
        </CardContent>
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{t('branches.newBranch')}</DialogTitle>
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
                  label={t('common.name')}
                  fullWidth
                  {...register('name')}
                  error={!!errors.name}
                  helperText={errors.name?.message}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label={t('branches.code')}
                  fullWidth
                  {...register('code')}
                  error={!!errors.code}
                  helperText={errors.code?.message}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label={t('branches.address')}
                  fullWidth
                  multiline
                  minRows={2}
                  {...register('address')}
                />
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
    </Box>
  );
}
