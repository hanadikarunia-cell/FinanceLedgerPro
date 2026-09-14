import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { alpha, useTheme } from '@mui/material/styles';
import type { ReactNode } from 'react';

interface StatCardProps {
  title: string;
  value: string;
  icon: ReactNode;
  color?: 'primary' | 'secondary' | 'success' | 'error' | 'warning' | 'info';
  subtitle?: string;
  onClick?: () => void;
}

export default function StatCard({
  title,
  value,
  icon,
  color = 'primary',
  subtitle,
  onClick,
}: StatCardProps) {
  const theme = useTheme();
  const paletteColor = theme.palette[color].main;

  return (
    <Card
      sx={{
        height: '100%',
        ...(onClick && {
          cursor: 'pointer',
          transition: 'box-shadow 0.15s, transform 0.15s',
          '&:hover': { boxShadow: 4, transform: 'translateY(-2px)' },
        }),
      }}
      onClick={onClick}
    >
      <CardContent
        sx={{ display: 'flex', alignItems: 'center', gap: 2, height: '100%' }}
      >
        <Avatar
          variant="rounded"
          sx={{
            bgcolor: alpha(paletteColor, 0.15),
            color: paletteColor,
            width: 52,
            height: 52,
          }}
        >
          {icon}
        </Avatar>
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="body2" color="text.secondary" noWrap>
            {title}
          </Typography>
          <Typography variant="h5" noWrap>
            {value}
          </Typography>
          {subtitle && (
            <Typography variant="caption" color="text.secondary">
              {subtitle}
            </Typography>
          )}
        </Box>
      </CardContent>
    </Card>
  );
}
