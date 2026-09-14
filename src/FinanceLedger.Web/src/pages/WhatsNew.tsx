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
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Grid from '@mui/material/Grid';
import IconButton from '@mui/material/IconButton';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AutoAwesomeIcon from '@mui/icons-material/AutoAwesome';

import ConfirmDialog from '@/components/ConfirmDialog';
import { useAuth } from '@/context/AuthContext';
import {
  useCreateReleaseNote,
  useDeleteReleaseNote,
  useReleaseNotes,
  useUpdateReleaseNote,
} from '@/hooks/useReleaseNotes';
import { APP_VERSION } from '@/utils/version';
import type { ReleaseNote, ReleaseType } from '@/types';
import { formatDateTime, getErrorMessage } from '@/utils/format';

const TYPE_COLOR: Record<ReleaseType, 'error' | 'info' | 'default'> = {
  Major: 'error',
  Minor: 'info',
  Patch: 'default',
};

export default function WhatsNew() {
  const { t } = useTranslation();
  const { isManager } = useAuth();

  const { data: notes = [], isLoading, isError, error } = useReleaseNotes();
  const createMut = useCreateReleaseNote();
  const updateMut = useUpdateReleaseNote();
  const deleteMut = useDeleteReleaseNote();

  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<ReleaseNote | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ReleaseNote | null>(null);

  const currentVersion = notes[0]?.version ?? APP_VERSION;

  const schema = useMemo(
    () =>
      z.object({
        version: z.string().min(1, t('whatsNew.versionRequired')),
        title: z.string().min(1, t('whatsNew.titleRequired')),
        type: z.enum(['Major', 'Minor', 'Patch']),
        notesText: z.string().min(1, t('whatsNew.notesRequired')),
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
    defaultValues: { version: '', title: '', type: 'Minor', notesText: '' },
  });

  useEffect(() => {
    if (!open) return;
    reset({
      version: editing?.version ?? '',
      title: editing?.title ?? '',
      type: editing?.type ?? 'Minor',
      notesText: editing?.notes.join('\n') ?? '',
    });
  }, [open, editing, reset]);

  const onSubmit = async (values: FormValues) => {
    const payload = {
      version: values.version,
      title: values.title,
      type: values.type,
      notes: values.notesText
        .split('\n')
        .map((n) => n.trim())
        .filter(Boolean),
    };
    if (editing) await updateMut.mutateAsync({ id: editing.id, payload });
    else await createMut.mutateAsync(payload);
    setOpen(false);
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    await deleteMut.mutateAsync(deleteTarget.id);
    setDeleteTarget(null);
  };

  const serverError =
    (createMut.isError && getErrorMessage(createMut.error)) ||
    (updateMut.isError && getErrorMessage(updateMut.error)) ||
    null;

  return (
    <Box>
      <Typography variant="h5">{t('whatsNew.title')}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        {t('whatsNew.subtitle')}
      </Typography>

      <Card sx={{ mb: 3, bgcolor: 'action.hover' }} variant="outlined">
        <CardContent
          sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2 }}
        >
          <Stack direction="row" spacing={2} alignItems="center">
            <AutoAwesomeIcon color="primary" fontSize="large" />
            <Box>
              <Typography variant="overline" color="primary.main" fontWeight={700}>
                {t('whatsNew.currentVersion')}
              </Typography>
              <Typography variant="h5" fontWeight={700}>
                v{currentVersion}
              </Typography>
            </Box>
          </Stack>
          {isManager && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => {
                setEditing(null);
                setOpen(true);
              }}
            >
              {t('whatsNew.publishUpdate')}
            </Button>
          )}
        </CardContent>
      </Card>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('whatsNew.failedToLoad'))}
        </Alert>
      )}
      {!isLoading && notes.length === 0 && !isError && (
        <Typography variant="body2" color="text.secondary">
          {t('whatsNew.empty')}
        </Typography>
      )}

      <Stack spacing={2}>
        {notes.map((note) => (
          <Card key={note.id} variant="outlined">
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                <Stack direction="row" spacing={1.5} alignItems="center" flexWrap="wrap">
                  <Chip
                    label={t(`whatsNew.type.${note.type}`)}
                    size="small"
                    color={TYPE_COLOR[note.type]}
                  />
                  <Typography variant="subtitle1" fontWeight={700}>
                    v{note.version}
                  </Typography>
                  <Typography variant="subtitle1" fontWeight={700}>
                    {note.title}
                  </Typography>
                </Stack>
                {isManager && (
                  <Stack direction="row" spacing={0.5}>
                    <IconButton
                      size="small"
                      onClick={() => {
                        setEditing(note);
                        setOpen(true);
                      }}
                    >
                      <EditIcon fontSize="small" />
                    </IconButton>
                    <IconButton size="small" onClick={() => setDeleteTarget(note)}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Stack>
                )}
              </Stack>

              <Typography variant="caption" color="text.secondary">
                {formatDateTime(note.publishedDate)} &middot;{' '}
                {t('whatsNew.publishedBy', { name: note.publishedByName })}
              </Typography>

              {note.notes.length > 0 && (
                <Box component="ul" sx={{ mt: 1, mb: 0, pl: 2.5 }}>
                  {note.notes.map((line, i) => (
                    <Typography key={i} component="li" variant="body2">
                      {line}
                    </Typography>
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>
        ))}
      </Stack>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editing ? t('whatsNew.editUpdate') : t('whatsNew.publishUpdate')}
        </DialogTitle>
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
                  label={t('whatsNew.version')}
                  fullWidth
                  placeholder="1.2.0"
                  {...register('version')}
                  error={!!errors.version}
                  helperText={errors.version?.message}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Controller
                  control={control}
                  name="type"
                  render={({ field }) => (
                    <TextField {...field} select label={t('whatsNew.updateType')} fullWidth>
                      {(['Major', 'Minor', 'Patch'] as ReleaseType[]).map((r) => (
                        <MenuItem key={r} value={r}>
                          {t(`whatsNew.type.${r}`)}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label={t('whatsNew.updateTitle')}
                  fullWidth
                  {...register('title')}
                  error={!!errors.title}
                  helperText={errors.title?.message}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label={t('whatsNew.notes')}
                  helperText={errors.notesText?.message ?? t('whatsNew.notesHint')}
                  error={!!errors.notesText}
                  fullWidth
                  multiline
                  minRows={4}
                  {...register('notesText')}
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

      <ConfirmDialog
        open={!!deleteTarget}
        title={t('whatsNew.deleteTitle')}
        message={t('whatsNew.deleteMessage', { version: deleteTarget?.version })}
        confirmLabel={t('common.delete')}
        confirmColor="error"
        loading={deleteMut.isPending}
        onConfirm={handleDelete}
        onClose={() => setDeleteTarget(null)}
      />
    </Box>
  );
}
