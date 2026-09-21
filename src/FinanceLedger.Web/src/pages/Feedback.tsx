import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Chip from '@mui/material/Chip';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import ImageIcon from '@mui/icons-material/Image';
import CloseIcon from '@mui/icons-material/Close';
import IconButton from '@mui/material/IconButton';

import type { GridColDef } from '@mui/x-data-grid';
import DataTable from '@/components/DataTable';
import { useAuth } from '@/context/AuthContext';
import { useCreateFeedback, useFeedbackList, useSetFeedbackSeverity } from '@/hooks/useFeedback';
import { filesApi } from '@/api/files';
import { APP_VERSION } from '@/utils/version';
import type { Feedback, FeedbackSeverity } from '@/types';
import { formatDateTime, getErrorMessage } from '@/utils/format';

export default function FeedbackPage() {
  const { t } = useTranslation();
  const { user } = useAuth();
  // Feedback goes to the application owner: only the Application Admin sees everyone's
  // and triages it (everyone else sees their own submissions).
  const isManager = user?.role === 'AppAdmin';

  const { data: items = [], isLoading, isError, error } = useFeedbackList();
  const createMut = useCreateFeedback();
  const severityMut = useSetFeedbackSeverity();

  const [message, setMessage] = useState('');
  const [imageUrl, setImageUrl] = useState<string | undefined>();
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const handlePasteImage = async (e: React.ClipboardEvent<HTMLTextAreaElement | HTMLInputElement>) => {
    const clipboardItems = e.clipboardData?.items;
    if (!clipboardItems) return;

    for (const item of Array.from(clipboardItems)) {
      if (item.type.startsWith('image/')) {
        const file = item.getAsFile();
        if (!file) continue;
        e.preventDefault();
        setUploadError(null);
        setUploading(true);
        try {
          const attachment = await filesApi.upload(file);
          setImageUrl(attachment.url);
        } catch (err) {
          setUploadError(getErrorMessage(err, t('feedback.uploadFailed')));
        } finally {
          setUploading(false);
        }
        break;
      }
    }
  };

  const handleSubmit = async () => {
    if (!message.trim()) return;
    await createMut.mutateAsync({ message: message.trim(), imageUrl, appVersion: APP_VERSION });
    setMessage('');
    setImageUrl(undefined);
  };

  const columns: GridColDef<Feedback>[] = [
    {
      field: 'submittedDate',
      headerName: t('common.date'),
      width: 160,
      valueFormatter: (value) => formatDateTime(value as string),
    },
    ...(isManager
      ? [
          {
            field: 'submittedByName',
            headerName: t('feedback.submittedBy'),
            width: 160,
          } satisfies GridColDef<Feedback>,
        ]
      : []),
    { field: 'message', headerName: t('feedback.message'), flex: 1, minWidth: 240 },
    {
      field: 'imageUrl',
      headerName: t('feedback.image'),
      width: 90,
      sortable: false,
      renderCell: (p) =>
        p.row.imageUrl ? (
          <a href={p.row.imageUrl} target="_blank" rel="noreferrer">
            <img
              src={p.row.imageUrl}
              alt=""
              style={{ height: 32, width: 32, objectFit: 'cover', borderRadius: 4 }}
            />
          </a>
        ) : null,
    },
    { field: 'appVersion', headerName: t('feedback.version'), width: 100 },
    {
      field: 'severity',
      headerName: t('feedback.severity'),
      width: 160,
      sortable: false,
      renderCell: (p) =>
        isManager ? (
          <Select
            size="small"
            variant="standard"
            value={p.row.severity ?? ''}
            displayEmpty
            onChange={(e) =>
              severityMut.mutate({
                id: p.row.id,
                severity: e.target.value as FeedbackSeverity,
              })
            }
            sx={{ minWidth: 110 }}
          >
            <MenuItem value="" disabled>
              {t('feedback.untriaged')}
            </MenuItem>
            <MenuItem value="Minor">{t('feedback.minor')}</MenuItem>
            <MenuItem value="Major">{t('feedback.major')}</MenuItem>
          </Select>
        ) : p.row.severity ? (
          <Chip
            label={t(`feedback.${p.row.severity.toLowerCase()}`)}
            size="small"
            color={p.row.severity === 'Major' ? 'error' : 'default'}
          />
        ) : (
          <Typography variant="caption" color="text.secondary">
            {t('feedback.untriaged')}
          </Typography>
        ),
    },
  ];

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        {t('feedback.title')}
      </Typography>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="subtitle1" sx={{ mb: 1 }}>
            {t('feedback.newFeedback')}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            {t('feedback.pasteHint')}
          </Typography>

          {uploadError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {uploadError}
            </Alert>
          )}
          {createMut.isError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {getErrorMessage(createMut.error)}
            </Alert>
          )}

          <TextField
            fullWidth
            multiline
            minRows={4}
            placeholder={t('feedback.messagePlaceholder')}
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            inputProps={{ onPaste: handlePasteImage }}
          />

          {imageUrl && (
            <Box sx={{ mt: 2, position: 'relative', display: 'inline-block' }}>
              <img
                src={imageUrl}
                alt=""
                style={{ maxHeight: 160, maxWidth: '100%', borderRadius: 8, display: 'block' }}
              />
              <IconButton
                size="small"
                onClick={() => setImageUrl(undefined)}
                sx={{
                  position: 'absolute',
                  top: 4,
                  right: 4,
                  bgcolor: 'background.paper',
                  boxShadow: 1,
                }}
              >
                <CloseIcon fontSize="small" />
              </IconButton>
            </Box>
          )}
          {uploading && (
            <Stack direction="row" spacing={1} alignItems="center" sx={{ mt: 1 }}>
              <ImageIcon fontSize="small" color="disabled" />
              <Typography variant="caption" color="text.secondary">
                {t('feedback.uploading')}
              </Typography>
            </Stack>
          )}

          <Stack direction="row" justifyContent="flex-end" sx={{ mt: 2 }}>
            <Button
              variant="contained"
              disabled={!message.trim() || createMut.isPending || uploading}
              onClick={handleSubmit}
            >
              {createMut.isPending ? t('common.saving') : t('feedback.submit')}
            </Button>
          </Stack>
        </CardContent>
      </Card>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('feedback.failedToLoad'))}
        </Alert>
      )}

      <Card>
        <CardContent>
          <DataTable
            rows={items}
            columns={columns}
            loading={isLoading}
            getRowId={(row) => row.id}
            getRowHeight={() => 'auto'}
          />
        </CardContent>
      </Card>
    </Box>
  );
}
