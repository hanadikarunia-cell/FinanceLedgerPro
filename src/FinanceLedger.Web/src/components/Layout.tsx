import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink, useLocation, useNavigate } from 'react-router-dom';
import AppBar from '@mui/material/AppBar';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import Drawer from '@mui/material/Drawer';
import IconButton from '@mui/material/IconButton';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Toolbar from '@mui/material/Toolbar';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import ListItem from '@mui/material/ListItem';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useTheme } from '@mui/material/styles';

import MenuIcon from '@mui/icons-material/Menu';
import DashboardIcon from '@mui/icons-material/Dashboard';
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong';
import AssessmentIcon from '@mui/icons-material/Assessment';
import PeopleIcon from '@mui/icons-material/People';
import StoreIcon from '@mui/icons-material/Store';
import HistoryIcon from '@mui/icons-material/History';
import SettingsIcon from '@mui/icons-material/Settings';
import PaidIcon from '@mui/icons-material/Paid';
import LightModeIcon from '@mui/icons-material/LightMode';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import LogoutIcon from '@mui/icons-material/Logout';
import AccountBalanceWalletIcon from '@mui/icons-material/AccountBalanceWallet';
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar';
import ReceiptIcon from '@mui/icons-material/Receipt';
import PaymentsIcon from '@mui/icons-material/Payments';

import { useAuth } from '@/context/AuthContext';
import { useColorMode } from '@/context/ThemeContext';
import LanguageSwitcher from '@/components/LanguageSwitcher';
import type { UserRole } from '@/types';

const DRAWER_WIDTH = 248;

interface NavItem {
  labelKey: string;
  to: string;
  icon: ReactNode;
  roles?: UserRole[];
}

const NAV_ITEMS: NavItem[] = [
  { labelKey: 'nav.dashboard', to: '/', icon: <DashboardIcon /> },
  { labelKey: 'nav.transactions', to: '/transactions', icon: <ReceiptLongIcon /> },
  { labelKey: 'pettyCash.title', to: '/petty-cash-requests', icon: <PaidIcon /> },
  { labelKey: 'nav.reports', to: '/reports', icon: <AssessmentIcon /> },
  { labelKey: 'nav.cars', to: '/cars', icon: <DirectionsCarIcon /> },
  { labelKey: 'invoices.title', to: '/invoices', icon: <ReceiptIcon />, roles: ['Manager'] },
  { labelKey: 'accountsPayable.title', to: '/accounts-payable', icon: <PaymentsIcon />, roles: ['Manager'] },
  { labelKey: 'nav.users', to: '/users', icon: <PeopleIcon />, roles: ['Manager'] },
  { labelKey: 'nav.branches', to: '/branches', icon: <StoreIcon />, roles: ['Manager'] },
  { labelKey: 'nav.auditLogs', to: '/audit-logs', icon: <HistoryIcon />, roles: ['Manager'] },
  { labelKey: 'nav.settings', to: '/settings', icon: <SettingsIcon /> },
];

export default function Layout({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
  const { mode, toggleColorMode } = useColorMode();
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const visibleItems = NAV_ITEMS.filter(
    (item) => !item.roles || (user && item.roles.includes(user.role)),
  );

  const handleLogout = () => {
    setAnchorEl(null);
    logout();
    navigate('/login', { replace: true });
  };

  const drawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Toolbar sx={{ gap: 1.5 }}>
        <AccountBalanceWalletIcon color="primary" />
        <Typography variant="h6" noWrap fontWeight={700}>
          {t('nav.brandShort')}
        </Typography>
      </Toolbar>
      <Divider />
      <List sx={{ px: 1, py: 1, flexGrow: 1 }}>
        {visibleItems.map((item) => {
          const selected =
            item.to === '/'
              ? location.pathname === '/'
              : location.pathname.startsWith(item.to);
          return (
            <ListItem key={item.to} disablePadding sx={{ mb: 0.5 }}>
              <ListItemButton
                component={RouterLink}
                to={item.to}
                selected={selected}
                onClick={() => !isDesktop && setMobileOpen(false)}
                sx={{ borderRadius: 2 }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                <ListItemText primary={t(item.labelKey)} />
              </ListItemButton>
            </ListItem>
          );
        })}
      </List>
      <Divider />
      <Box sx={{ p: 2 }}>
        <Typography variant="caption" color="text.secondary">
          {t('nav.version')}
        </Typography>
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        position="fixed"
        color="default"
        elevation={0}
        sx={{
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          ml: { md: `${DRAWER_WIDTH}px` },
          borderBottom: 1,
          borderColor: 'divider',
          bgcolor: 'background.paper',
        }}
      >
        <Toolbar>
          <IconButton
            edge="start"
            onClick={() => setMobileOpen((o) => !o)}
            sx={{ mr: 2, display: { md: 'none' } }}
            aria-label={t('layout.openNavigation')}
          >
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" sx={{ flexGrow: 1 }} noWrap>
            {t(
              visibleItems.find((i) =>
                i.to === '/' ? location.pathname === '/' : location.pathname.startsWith(i.to),
              )?.labelKey ?? 'nav.appName',
            )}
          </Typography>

          <LanguageSwitcher />

          <Tooltip title={mode === 'light' ? t('layout.darkMode') : t('layout.lightMode')}>
            <IconButton onClick={toggleColorMode} aria-label={t('layout.toggleColorMode')}>
              {mode === 'light' ? <DarkModeIcon /> : <LightModeIcon />}
            </IconButton>
          </Tooltip>

          <Tooltip title={t('layout.account')}>
            <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} sx={{ ml: 1 }}>
              <Avatar sx={{ width: 34, height: 34, bgcolor: 'primary.main' }}>
                {user?.displayName?.charAt(0)?.toUpperCase() ?? 'U'}
              </Avatar>
            </IconButton>
          </Tooltip>

          <Menu
            anchorEl={anchorEl}
            open={!!anchorEl}
            onClose={() => setAnchorEl(null)}
            anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            transformOrigin={{ vertical: 'top', horizontal: 'right' }}
          >
            <Box sx={{ px: 2, py: 1 }}>
              <Typography variant="subtitle2">{user?.displayName}</Typography>
              <Typography variant="caption" color="text.secondary">
                {user?.email} · {user?.role}
              </Typography>
            </Box>
            <Divider />
            <MenuItem
              component={RouterLink}
              to="/settings"
              onClick={() => setAnchorEl(null)}
            >
              <ListItemIcon>
                <SettingsIcon fontSize="small" />
              </ListItemIcon>
              {t('layout.settings')}
            </MenuItem>
            <MenuItem onClick={handleLogout}>
              <ListItemIcon>
                <LogoutIcon fontSize="small" />
              </ListItemIcon>
              {t('layout.logout')}
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      <Box
        component="nav"
        sx={{ width: { md: DRAWER_WIDTH }, flexShrink: { md: 0 } }}
        aria-label="navigation"
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: DRAWER_WIDTH },
          }}
        >
          {drawerContent}
        </Drawer>
        <Drawer
          variant="permanent"
          open
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': {
              boxSizing: 'border-box',
              width: DRAWER_WIDTH,
              borderRight: 1,
              borderColor: 'divider',
            },
          }}
        >
          {drawerContent}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          p: { xs: 2, sm: 3 },
        }}
      >
        <Toolbar />
        {children}
      </Box>
    </Box>
  );
}
