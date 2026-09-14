import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { IconButton } from 'react-native-paper';
import { useAuth } from '@/auth/AuthContext';
import { DashboardScreen } from '@/screens/DashboardScreen';
import { IncomeEntryScreen } from '@/screens/IncomeEntryScreen';
import { ExpenseEntryScreen } from '@/screens/ExpenseEntryScreen';
import { ReportsScreen } from '@/screens/ReportsScreen';
import { palette } from '@/theme';
import type { MainTabParamList } from './types';

const Tab = createBottomTabNavigator<MainTabParamList>();

const ICONS: Record<keyof MainTabParamList, string> = {
  Dashboard: 'view-dashboard',
  Income: 'cash-plus',
  Expense: 'cash-minus',
  Reports: 'chart-bar',
};

export function MainTabs() {
  const { logout } = useAuth();

  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarActiveTintColor: palette.primary,
        tabBarIcon: ({ color, size }) => (
          <IconButton
            icon={ICONS[route.name]}
            iconColor={color}
            size={size}
            style={{ margin: 0 }}
          />
        ),
        headerRight: () => (
          <IconButton icon="logout" onPress={() => void logout()} />
        ),
      })}
    >
      <Tab.Screen name="Dashboard" component={DashboardScreen} />
      <Tab.Screen
        name="Income"
        component={IncomeEntryScreen}
        options={{ title: 'Income' }}
      />
      <Tab.Screen
        name="Expense"
        component={ExpenseEntryScreen}
        options={{ title: 'Expense' }}
      />
      <Tab.Screen name="Reports" component={ReportsScreen} />
    </Tab.Navigator>
  );
}
