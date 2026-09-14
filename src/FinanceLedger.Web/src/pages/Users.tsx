import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Controller, useForm } from 'react-hook-form';
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
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import Chip from '@mui/material/Chip';
import IconButton from '@mui/material/IconButton';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';

import type { GridColDef } from '@mui/x-data-grid';
import DataTable from '@/components/DataTable';
import { useBranches } from '@/hooks/useBranches';
import { useCreateUser, useUpdateUser, useUsers } from '@/hooks/useUsers';
import type { User, UserInput, UserRole } from '@/types';
import { getErrorMessage } from '@/utils/format';

export default function Users() {
  const { t } = useTranslation();
  const { data: users = [], isLoading, isError, error } = useUsers();
  const { data: branches = [] } = useBranches();
  const createMut = useCreateUser();
  const updateMut = useUpdateUser();

  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<User | null>(null);

  const schema = useMemo(
    () =>
      z.object({
        email: z.string().min(1, t('users.emailRequired')).email(t('users.emailInvalid')),
        displayName: z.string().min(1, t('users.nameRequired')),
        role: z.enum(['Manager', 'User']),
        assignedBranches: z.array(z.string()),
        password: z.string().optional(),
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
      email: '',
      displayName: '',
      role: 'User',
      assignedBranches: [],
      password: '',
    },
  });

  useEffect(() => {
    if (!open) return;
    reset({
      email: editing?.email ?? '',
      displayName: editing?.displayName ?? '',
      role: editing?.role ?? 'User',
      assignedBranches: editing?.assignedBranches ?? [],
      password: '',
    });
  }, [open, editing, reset]);

  const onSubmit = async (values: FormValues) => {
    const payload: UserInput = {
      email: values.email,
      displayName: values.displayName,
      role: values.role,
      assignedBranches: values.assignedBranches,
      password: values.password || undefined,
    };
    if (editing) await updateMut.mutateAsync({ id: editing.id, payload });
    else await createMut.mutateAsync(payload);
    setOpen(false);
  };

  const columns: GridColDef<User>[] = [
    { field: 'displayName', headerName: t('common.name'), flex: 1, minWidth: 160 },
    { field: 'email', headerName: t('common.email'), flex: 1, minWidth: 200 },
    {
      field: 'role',
      headerName: t('users.role'),
      width: 130,
      renderCell: (p) => (
        <Chip
          label={t(`enums.role.${p.row.role}`)}
          size="small"
          color={p.row.role === 'Manager' ? 'primary' : 'default'}
        />
      ),
    },
    {
      field: 'assignedBranches',
      headerName: t('users.branches'),
      flex: 1,
      minWidth: 160,
      valueGetter: (value) => (value as string[])?.length ?? 0,
      renderCell: (p) => (
        <Typography variant="body2">
          {(p.row.assignedBranches ?? []).length} {t('users.assigned')}
        </Typography>
      ),
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
        <Typography variant="h5">{t('users.title')}</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => {
            setEditing(null);
            setOpen(true);
          }}
        >
          {t('users.newUser')}
        </Button>
      </Stack>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('users.failedToLoad'))}
        </Alert>
      )}

      <Card>
        <CardContent>
          <DataTable
            rows={users}
            columns={columns}
            loading={isLoading}
            getRowId={(row) => row.id}
          />
        </CardContent>
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editing ? t('users.editUser') : t('users.newUser')}</DialogTitle>
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
                  label={t('users.displayName')}
                  fullWidth
                  {...register('displayName')}
                  error={!!errors.displayName}
                  helperText={errors.displayName?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('common.email')}
                  type="email"
                  fullWidth
                  {...register('email')}
                  error={!!errors.email}
                  helperText={errors.email?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Controller
                  control={control}
                  name="role"
                  render={({ field }) => (
                    <TextField {...field} select label={t('users.role')} fullWidth>
                      {(['Manager', 'User'] as UserRole[]).map((r) => (
                        <MenuItem key={r} value={r}>
                          {t(`enums.role.${r}`)}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={editing ? t('users.resetPasswordOptional') : t('common.password')}
                  type="password"
                  fullWidth
                  {...register('password')}
                  error={!!errors.password}
                  helperText={errors.password?.message}
                />
              </Grid>
              <Grid item xs={12}>
                <Controller
                  control={control}
                  name="assignedBranches"
                  render={({ field }) => (
                    <TextField
                      select
                      label={t('users.assignedBranches')}
                      fullWidth
                      SelectProps={{
                        multiple: true,
                        value: field.value,
                        onChange: (e) =>
                          field.onChange(
                            typeof e.target.value === 'string'
                              ? e.target.value.split(',')
                              : e.target.value,
                          ),
                        renderValue: (selected) =>
                          (selected as string[])
                            .map((id) => branches.find((b) => b.id === id)?.name ?? id)
                            .join(', '),
                      }}
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
