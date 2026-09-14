import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Divider from '@mui/material/Divider';
import FormControlLabel from '@mui/material/FormControlLabel';
import Grid from '@mui/material/Grid';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { useAuth } from '@/context/AuthContext';
import { useColorMode } from '@/context/ThemeContext';

export default function Settings() {
  const { t, i18n } = useTranslation();
  const { user, updateUser } = useAuth();
  const { mode, toggleColorMode } = useColorMode();

  const profileSchema = useMemo(
    () =>
      z.object({
        displayName: z.string().min(1, t('settings.nameRequired')),
        email: z.string().email(),
      }),
    [t],
  );
  type ProfileForm = z.infer<typeof profileSchema>;

  const passwordSchema = useMemo(
    () =>
      z
        .object({
          currentPassword: z.string().min(1, t('settings.currentPasswordRequired')),
          newPassword: z.string().min(8, t('settings.passwordMinLength')),
          confirmPassword: z.string().min(1, t('settings.confirmPasswordRequired')),
        })
        .refine((v) => v.newPassword === v.confirmPassword, {
          message: t('settings.passwordsDoNotMatch'),
          path: ['confirmPassword'],
        }),
    [t],
  );
  type PasswordForm = z.infer<typeof passwordSchema>;

  const profileForm = useForm<ProfileForm>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      displayName: user?.displayName ?? '',
      email: user?.email ?? '',
    },
  });

  const passwordForm = useForm<PasswordForm>({
    resolver: zodResolver(passwordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  const onSaveProfile = (values: ProfileForm) => {
    if (user) updateUser({ ...user, displayName: values.displayName });
  };

  const onChangePassword = () => {
    // Wire up to POST /auth/change-password when available on the API.
    passwordForm.reset();
  };

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        {t('settings.title')}
      </Typography>

      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader title={t('settings.appearance')} />
            <Divider />
            <CardContent>
              <FormControlLabel
                control={<Switch checked={mode === 'dark'} onChange={toggleColorMode} />}
                label={t('settings.darkMode')}
              />
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                {t('settings.themeSaved')}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader title={t('settings.language')} />
            <Divider />
            <CardContent>
              <TextField
                select
                label={t('settings.language')}
                value={i18n.resolvedLanguage}
                onChange={(e) => void i18n.changeLanguage(e.target.value)}
                sx={{ minWidth: 220 }}
              >
                <MenuItem value="en">English</MenuItem>
                <MenuItem value="id">Bahasa Indonesia</MenuItem>
              </TextField>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                {t('settings.languageSaved')}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader title={t('settings.profile')} />
            <Divider />
            <CardContent>
              <Box
                component="form"
                onSubmit={profileForm.handleSubmit(onSaveProfile)}
                noValidate
              >
                <Stack spacing={2}>
                  <TextField
                    label={t('users.displayName')}
                    fullWidth
                    {...profileForm.register('displayName')}
                    error={!!profileForm.formState.errors.displayName}
                    helperText={profileForm.formState.errors.displayName?.message}
                  />
                  <TextField
                    label={t('common.email')}
                    fullWidth
                    disabled
                    {...profileForm.register('email')}
                  />
                  <Box>
                    <Button type="submit" variant="contained">
                      {t('settings.saveProfile')}
                    </Button>
                  </Box>
                </Stack>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader title={t('settings.changePassword')} />
            <Divider />
            <CardContent>
              {passwordForm.formState.isSubmitSuccessful && (
                <Alert severity="success" sx={{ mb: 2 }}>
                  {t('settings.passwordChangeRequested')}
                </Alert>
              )}
              <Box
                component="form"
                onSubmit={passwordForm.handleSubmit(onChangePassword)}
                noValidate
              >
                <Stack spacing={2}>
                  <TextField
                    label={t('settings.currentPassword')}
                    type="password"
                    fullWidth
                    {...passwordForm.register('currentPassword')}
                    error={!!passwordForm.formState.errors.currentPassword}
                    helperText={passwordForm.formState.errors.currentPassword?.message}
                  />
                  <TextField
                    label={t('settings.newPassword')}
                    type="password"
                    fullWidth
                    {...passwordForm.register('newPassword')}
                    error={!!passwordForm.formState.errors.newPassword}
                    helperText={passwordForm.formState.errors.newPassword?.message}
                  />
                  <TextField
                    label={t('settings.confirmNewPassword')}
                    type="password"
                    fullWidth
                    {...passwordForm.register('confirmPassword')}
                    error={!!passwordForm.formState.errors.confirmPassword}
                    helperText={passwordForm.formState.errors.confirmPassword?.message}
                  />
                  <Box>
                    <Button type="submit" variant="contained">
                      {t('settings.updatePassword')}
                    </Button>
                  </Box>
                </Stack>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
