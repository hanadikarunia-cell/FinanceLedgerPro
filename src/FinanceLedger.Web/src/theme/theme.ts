import { createTheme, type Theme } from '@mui/material/styles';
import type { PaletteMode } from '@mui/material';

export const createAppTheme = (mode: PaletteMode): Theme =>
  createTheme({
    palette: {
      mode,
      primary: {
        main: '#1976d2',
      },
      secondary: {
        main: '#00897b',
      },
      success: { main: '#2e7d32' },
      error: { main: '#d32f2f' },
      warning: { main: '#ed6c02' },
      background:
        mode === 'light'
          ? { default: '#f4f6f8', paper: '#ffffff' }
          : { default: '#121212', paper: '#1e1e1e' },
    },
    shape: {
      borderRadius: 10,
    },
    typography: {
      fontFamily: [
        'Inter',
        'Roboto',
        '-apple-system',
        'BlinkMacSystemFont',
        'Segoe UI',
        'Arial',
        'sans-serif',
      ].join(','),
      h4: { fontWeight: 700 },
      h5: { fontWeight: 700 },
      h6: { fontWeight: 600 },
    },
    components: {
      MuiCard: {
        styleOverrides: {
          root: {
            boxShadow:
              mode === 'light'
                ? '0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.06)'
                : '0 1px 3px rgba(0,0,0,0.4)',
          },
        },
      },
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: { root: { textTransform: 'none', fontWeight: 600 } },
      },
    },
  });
