import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import LinearProgress from '@mui/material/LinearProgress';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import { filesApi } from '@/api/files';
import type { Attachment } from '@/types';
import { getErrorMessage } from '@/utils/format';

interface FileUploadProps {
  attachments: Attachment[];
  onChange: (attachments: Attachment[]) => void;
  accept?: string;
  disabled?: boolean;
}

export default function FileUpload({
  attachments,
  onChange,
  accept = '.pdf,.png,.jpg,.jpeg,.xlsx,.csv,.doc,.docx',
  disabled,
}: FileUploadProps) {
  const { t } = useTranslation();
  const inputRef = useRef<HTMLInputElement>(null);
  const [progress, setProgress] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleFile = async (file: File) => {
    setError(null);
    setProgress(0);
    try {
      const uploaded = await filesApi.upload(file, setProgress);
      onChange([...attachments, uploaded]);
    } catch (err) {
      setError(getErrorMessage(err, t('transactionForm.uploadFailed')));
    } finally {
      setProgress(null);
    }
  };

  const handleRemove = (id: string) => {
    onChange(attachments.filter((a) => a.id !== id));
  };

  return (
    <Box>
      <input
        ref={inputRef}
        type="file"
        accept={accept}
        hidden
        onChange={(e) => {
          const file = e.target.files?.[0];
          if (file) void handleFile(file);
          e.target.value = '';
        }}
      />
      <Button
        variant="outlined"
        startIcon={<CloudUploadIcon />}
        onClick={() => inputRef.current?.click()}
        disabled={disabled || progress !== null}
        size="small"
      >
        {t('transactionForm.attachFile')}
      </Button>

      {progress !== null && (
        <LinearProgress variant="determinate" value={progress} sx={{ mt: 1 }} />
      )}
      {error && (
        <Typography variant="caption" color="error" sx={{ display: 'block', mt: 0.5 }}>
          {error}
        </Typography>
      )}

      {attachments.length > 0 && (
        <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: 'wrap', gap: 1 }}>
          {attachments.map((a) => (
            <Chip
              key={a.id}
              label={a.fileName}
              onDelete={disabled ? undefined : () => handleRemove(a.id)}
              component="a"
              href={a.url}
              target="_blank"
              clickable
              size="small"
            />
          ))}
        </Stack>
      )}
    </Box>
  );
}
