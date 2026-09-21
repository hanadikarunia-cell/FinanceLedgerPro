import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import Alert from '@mui/material/Alert';
import Autocomplete from '@mui/material/Autocomplete';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControlLabel from '@mui/material/FormControlLabel';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { useAuth } from '@/context/AuthContext';
import { useSites, useSiteUsers } from '@/hooks/useSites';
import { usersApi } from '@/api/users';
import type { User } from '@/types';
import { getErrorMessage } from '@/utils/format';

interface ViewAsDialogProps {
  open: boolean;
  onClose: () => void;
  /** Pre-select a site (Application Admin), e.g. from the Sites page. */
  initialSiteId?: string | null;
}

/**
 * Pick a person and use the app as them. The Application Admin chooses a site and then
 * anyone in it; a Site Admin chooses among the regular users of their own site. The
 * server enforces the same limits; this list only saves the admin from guessing.
 */
export default function ViewAsDialog({ open, onClose, initialSiteId = null }: ViewAsDialogProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { realUser, startActingAs } = useAuth();
  const isAppAdmin = realUser?.role === 'AppAdmin';

  const [siteId, setSiteId] = useState<string | null>(initialSiteId);
  const [target, setTarget] = useState<User | null>(null);
  const [canWrite, setCanWrite] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sites = useSites(open && isAppAdmin);
  const siteUsers = useSiteUsers(isAppAdmin ? siteId : null);
  const ownSiteUsers = useQuery({
    queryKey: ['users', 'view-as'],
    queryFn: () => usersApi.list(),
    enabled: open && !isAppAdmin,
  });

  useEffect(() => {
    if (open) {
      setSiteId(initialSiteId);
      setTarget(null);
      setCanWrite(false);
      setError(null);
    }
  }, [open, initialSiteId]);

  const candidates = useMemo(() => {
    const list = isAppAdmin ? siteUsers.data : ownSiteUsers.data;
    return (list ?? []).filter(
      (u) => u.isActive !== false && u.role !== 'AppAdmin' && (isAppAdmin || u.role === 'User'),
    );
  }, [isAppAdmin, siteUsers.data, ownSiteUsers.data]);

  const loadingUsers = isAppAdmin ? siteUsers.isFetching : ownSiteUsers.isFetching;

  const handleStart = async () => {
    if (!target) return;
    setSubmitting(true);
    setError(null);
    try {
      await startActingAs(target, canWrite);
      onClose();
      navigate('/', { replace: true });
    } catch (err) {
      setError(getErrorMessage(err, t('viewAs.failed')));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{t('viewAs.title')}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2.5} sx={{ pt: 0.5 }}>
          <Typography variant="body2" color="text.secondary">
            {isAppAdmin ? t('viewAs.introAppAdmin') : t('viewAs.introSiteAdmin')}
          </Typography>

          {error && <Alert severity="error">{error}</Alert>}

          {isAppAdmin && (
            <TextField
              select
              label={t('viewAs.site')}
              value={siteId ?? ''}
              onChange={(e) => {
                setSiteId(e.target.value || null);
                setTarget(null);
              }}
              fullWidth
            >
              {(sites.data ?? []).map((s) => (
                <MenuItem key={s.id} value={s.id}>
                  {s.name} ({s.code})
                </MenuItem>
              ))}
            </TextField>
          )}

          <Autocomplete
            options={candidates}
            value={target}
            onChange={(_, value) => setTarget(value)}
            loading={loadingUsers}
            disabled={isAppAdmin && !siteId}
            getOptionLabel={(u) => `${u.displayName} - ${u.email}`}
            groupBy={(u) => t(`enums.role.${u.role}`)}
            isOptionEqualToValue={(a, b) => a.id === b.id}
            noOptionsText={t('viewAs.noUsers')}
            renderInput={(params) => <TextField {...params} label={t('viewAs.user')} />}
          />

          <div>
            <FormControlLabel
              control={
                <Switch checked={canWrite} onChange={(e) => setCanWrite(e.target.checked)} />
              }
              label={t('viewAs.allowChanges')}
            />
            <Typography variant="caption" color="text.secondary" display="block">
              {canWrite ? t('viewAs.allowChangesOn') : t('viewAs.allowChangesOff')}
            </Typography>
          </div>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={submitting}>
          {t('common.cancel')}
        </Button>
        <Button variant="contained" onClick={handleStart} disabled={!target || submitting}>
          {t('viewAs.start')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
