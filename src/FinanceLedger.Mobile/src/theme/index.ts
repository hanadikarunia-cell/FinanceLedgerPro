import { MD3LightTheme, MD3DarkTheme, type MD3Theme } from 'react-native-paper';

const brand = {
  primary: '#2563eb',
  income: '#16a34a',
  expense: '#dc2626',
  slate: '#1e293b',
};

export const lightTheme: MD3Theme = {
  ...MD3LightTheme,
  colors: {
    ...MD3LightTheme.colors,
    primary: brand.primary,
    secondary: brand.slate,
  },
};

export const darkTheme: MD3Theme = {
  ...MD3DarkTheme,
  colors: {
    ...MD3DarkTheme.colors,
    primary: brand.primary,
  },
};

export const palette = brand;
