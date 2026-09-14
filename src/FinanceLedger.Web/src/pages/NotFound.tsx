import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import { Link as RouterLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

export default function NotFound() {
  const { t } = useTranslation();
  return (
    <Box
      sx={{
        minHeight: '70vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        textAlign: 'center',
        gap: 2,
      }}
    >
      <Typography variant="h1" color="primary" fontWeight={800}>
        404
      </Typography>
      <Typography variant="h6">{t('notFound.title')}</Typography>
      <Typography variant="body2" color="text.secondary">
        {t('notFound.message')}
      </Typography>
      <Button component={RouterLink} to="/" variant="contained">
        {t('notFound.backToDashboard')}
      </Button>
    </Box>
  );
}
