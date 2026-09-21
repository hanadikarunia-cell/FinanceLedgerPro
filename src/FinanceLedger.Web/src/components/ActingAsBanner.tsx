import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import FormControlLabel from '@mui/material/FormControlLabel';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';

import { useAuth } from '@/context/AuthContext';
import ConfirmDialog from '@/components/ConfirmDialog';

/**
 * Always visible while "View as" is on, so an admin can never forget whose eyes they are
 * looking through, or that changes made now are made as that person.
 */
export default function ActingAsBanner() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user, actingAs, setActingCanWrite, stopActingAs } = useAuth();
  const [confirmOpen, setConfirmOpen] = useState(false);

  if (!actingAs || !user) return null;

  const who = user.tenantName ? `${user.displayName} (${user.tenantName})` : user.displayName;

  const handleExit = () => {
    stopActingAs();
    navigate('/', { replace: true });
  };

  return (
    <>
      <Alert
        severity={actingAs.canWrite ? 'error' : 'warning'}
        icon={false}
        sx={{ mb: 2, position: 'sticky', top: { xs: 64, sm: 72 }, zIndex: 5, alignItems: 'center' }}
        action={
          <Button color="inherit" size="small" onClick={handleExit} sx={{ fontWeight: 700 }}>
            {t('viewAs.exit')}
          </Button>
        }
      >
        <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap" useFlexGap>
          <span>
            {t(actingAs.canWrite ? 'viewAs.bannerWrite' : 'viewAs.bannerReadOnly', {
              name: who,
              admin: actingAs.realUser.displayName,
            })}
          </span>
          <FormControlLabel
            control={
              <Switch
                size="small"
                checked={actingAs.canWrite}
                onChange={(e) =>
                  e.target.checked ? setConfirmOpen(true) : setActingCanWrite(false)
                }
              />
            }
            label={t('viewAs.allowChanges')}
          />
        </Stack>
      </Alert>

      <ConfirmDialog
        open={confirmOpen}
        title={t('viewAs.confirmWriteTitle')}
        message={t('viewAs.confirmWriteMessage', { name: user.displayName })}
        confirmColor="warning"
        onConfirm={() => {
          setActingCanWrite(true);
          setConfirmOpen(false);
        }}
        onClose={() => setConfirmOpen(false)}
      />
    </>
  );
}
