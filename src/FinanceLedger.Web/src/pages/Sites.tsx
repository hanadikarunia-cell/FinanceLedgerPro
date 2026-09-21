import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
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
import FormControlLabel from '@mui/material/FormControlLabel';
import Grid from '@mui/material/Grid';
import IconButton from '@mui/material/IconButton';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import VisibilityIcon from '@mui/icons-material/Visibility';

import type { GridColDef } from '@mui/x-data-grid';
import DataTable from '@/components/DataTable';
import ViewAsDialog from '@/components/ViewAsDialog';
import { useCreateSite, useSites, useUpdateSite } from '@/hooks/useSites';
import type { Site } from '@/types';
import { formatDateTime, getErrorMessage } from '@/utils/format';

const createSchemaFactory = (t: (k: string) => string) =>
  z.object({
    name: z.string().min(1, t('sites.nameRequired')),
    code: z
      .string()
      .min(1, t('sites.codeRequired'))
      .max(20)
      .regex(/^[A-Za-z0-9_-]+$/, t('sites.codeInvalid')),
    adminDisplayName: z.string().min(1, t('sites.adminNameRequired')),
    adminEmail: z.string().email(t('sites.adminEmailInvalid')),
    adminPassword: z.string().min(8, t('sites.adminPasswordShort')),
  });

type CreateValues = z.infer<ReturnType<typeof createSchemaFactory>>;

const EMPTY: CreateValues = {
  name: '',
  code: '',
  adminDisplayName: '',
  adminEmail: '',
  adminPassword: '',
};

/** Application Admin's home: the client sites, their admins, and a way in to look around. */
export default function Sites() {
  const { t } = useTranslation();
  const { data: sites = [], isLoading, isError, error } = useSites();
  const createMut = useCreateSite();
  const updateMut = useUpdateSite();

  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<Site | null>(null);
  const [editName, setEditName] = useState('');
  const [editActive, setEditActive] = useState(true);
  const [viewAsSiteId, setViewAsSiteId] = useState<string | null>(null);
  const [viewAsOpen, setViewAsOpen] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateValues>({
    resolver: zodResolver(createSchemaFactory(t)),
    defaultValues: EMPTY,
  });

  useEffect(() => {
    if (createOpen) {
      reset(EMPTY);
      createMut.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [createOpen, reset]);

  useEffect(() => {
    if (editing) {
      setEditName(editing.name);
      setEditActive(editing.isActive);
      updateMut.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [editing]);

  const onCreate = async (values: CreateValues) => {
    await createMut.mutateAsync(values);
    setCreateOpen(false);
  };

  const onSaveEdit = async () => {
    if (!editing || !editName.trim()) return;
    await updateMut.mutateAsync({
      id: editing.id,
      payload: { name: editName.trim(), isActive: editActive },
    });
    setEditing(null);
  };

  const columns: GridColDef<Site>[] = [
    { field: 'name', headerName: t('common.name'), flex: 1, minWidth: 180 },
    { field: 'code', headerName: t('sites.code'), width: 140 },
    { field: 'userCount', headerName: t('sites.users'), width: 110, type: 'number' },
    {
      field: 'isActive',
      headerName: t('common.status'),
      width: 130,
      renderCell: (p) => (
        <Chip
          size="small"
          label={p.row.isActive ? t('sites.active') : t('sites.inactive')}
          color={p.row.isActive ? 'success' : 'default'}
        />
      ),
    },
    {
      field: 'createdDate',
      headerName: t('sites.created'),
      width: 180,
      valueFormatter: (value) => formatDateTime(value as string),
    },
    {
      field: 'actions',
      headerName: '',
      width: 110,
      sortable: false,
      renderCell: (p) => (
        <Stack direction="row" spacing={0.5}>
          <Tooltip title={t('sites.edit')}>
            <IconButton size="small" onClick={() => setEditing(p.row)}>
              <EditIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title={t('viewAs.menu')}>
            <span>
              <IconButton
                size="small"
                disabled={!p.row.isActive}
                onClick={() => {
                  setViewAsSiteId(p.row.id);
                  setViewAsOpen(true);
                }}
              >
                <VisibilityIcon fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
        </Stack>
      ),
    },
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" sx={{ mb: 1 }}>
        <Typography variant="h5">{t('sites.title')}</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
          {t('sites.newSite')}
        </Button>
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        {t('sites.subtitle')}
      </Typography>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('sites.failedToLoad'))}
        </Alert>
      )}

      <Card>
        <CardContent>
          <DataTable rows={sites} columns={columns} loading={isLoading} getRowId={(r) => r.id} />
        </CardContent>
      </Card>

      {/* New site */}
      <Dialog open={createOpen} onClose={() => setCreateOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{t('sites.newSite')}</DialogTitle>
        <form onSubmit={handleSubmit(onCreate)} noValidate>
          <DialogContent dividers>
            {createMut.isError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {getErrorMessage(createMut.error)}
              </Alert>
            )}
            <Grid container spacing={2}>
              <Grid item xs={12} sm={8}>
                <TextField
                  label={t('sites.siteName')}
                  fullWidth
                  {...register('name')}
                  error={!!errors.name}
                  helperText={errors.name?.message}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  label={t('sites.code')}
                  fullWidth
                  {...register('code')}
                  error={!!errors.code}
                  helperText={errors.code?.message}
                />
              </Grid>
              <Grid item xs={12}>
                <Typography variant="subtitle2" color="text.secondary">
                  {t('sites.firstAdmin')}
                </Typography>
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label={t('sites.adminName')}
                  fullWidth
                  {...register('adminDisplayName')}
                  error={!!errors.adminDisplayName}
                  helperText={errors.adminDisplayName?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('common.email')}
                  type="email"
                  fullWidth
                  {...register('adminEmail')}
                  error={!!errors.adminEmail}
                  helperText={errors.adminEmail?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label={t('common.password')}
                  type="password"
                  autoComplete="new-password"
                  fullWidth
                  {...register('adminPassword')}
                  error={!!errors.adminPassword}
                  helperText={errors.adminPassword?.message}
                />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button onClick={() => setCreateOpen(false)} disabled={isSubmitting}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {t('common.create')}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Edit site */}
      <Dialog open={!!editing} onClose={() => setEditing(null)} maxWidth="xs" fullWidth>
        <DialogTitle>{t('sites.edit')}</DialogTitle>
        <DialogContent dividers>
          {updateMut.isError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {getErrorMessage(updateMut.error)}
            </Alert>
          )}
          <Stack spacing={2} sx={{ pt: 0.5 }}>
            <TextField
              label={t('sites.siteName')}
              value={editName}
              onChange={(e) => setEditName(e.target.value)}
              fullWidth
            />
            <FormControlLabel
              control={
                <Switch checked={editActive} onChange={(e) => setEditActive(e.target.checked)} />
              }
              label={t('sites.active')}
            />
            {!editActive && (
              <Typography variant="caption" color="text.secondary">
                {t('sites.inactiveHint')}
              </Typography>
            )}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button onClick={() => setEditing(null)}>{t('common.cancel')}</Button>
          <Button
            variant="contained"
            onClick={onSaveEdit}
            disabled={updateMut.isPending || !editName.trim()}
          >
            {t('common.save')}
          </Button>
        </DialogActions>
      </Dialog>

      <ViewAsDialog
        open={viewAsOpen}
        onClose={() => setViewAsOpen(false)}
        initialSiteId={viewAsSiteId}
      />
    </Box>
  );
}
