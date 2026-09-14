import { Routes, Route, Navigate } from 'react-router-dom';

import ProtectedRoute from '@/components/ProtectedRoute';
import RoleGuard from '@/components/RoleGuard';
import Layout from '@/components/Layout';

import Login from '@/pages/Login';
import Dashboard from '@/pages/Dashboard';
import Transactions from '@/pages/Transactions';
import PettyCashRequests from '@/pages/PettyCashRequests';
import Cars from '@/pages/Cars';
import Reports from '@/pages/Reports';
import Users from '@/pages/Users';
import Branches from '@/pages/Branches';
import Invoices from '@/pages/Invoices';
import AccountsPayable from '@/pages/AccountsPayable';
import AuditLogs from '@/pages/AuditLogs';
import Settings from '@/pages/Settings';
import NotFound from '@/pages/NotFound';

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />

      <Route element={<ProtectedRoute />}>
        <Route
          path="/"
          element={
            <Layout>
              <Dashboard />
            </Layout>
          }
        />
        <Route
          path="/transactions"
          element={
            <Layout>
              <Transactions />
            </Layout>
          }
        />
        <Route
          path="/petty-cash-requests"
          element={
            <Layout>
              <PettyCashRequests />
            </Layout>
          }
        />
        <Route
          path="/reports"
          element={
            <Layout>
              <Reports />
            </Layout>
          }
        />
        <Route
          path="/cars"
          element={
            <Layout>
              <Cars />
            </Layout>
          }
        />
        <Route
          path="/settings"
          element={
            <Layout>
              <Settings />
            </Layout>
          }
        />

        {/* Manager-only routes */}
        <Route element={<RoleGuard allow={['Manager']} />}>
          <Route
            path="/users"
            element={
              <Layout>
                <Users />
              </Layout>
            }
          />
          <Route
            path="/branches"
            element={
              <Layout>
                <Branches />
              </Layout>
            }
          />
          <Route
            path="/audit-logs"
            element={
              <Layout>
                <AuditLogs />
              </Layout>
            }
          />
          <Route
            path="/invoices"
            element={
              <Layout>
                <Invoices />
              </Layout>
            }
          />
          <Route
            path="/accounts-payable"
            element={
              <Layout>
                <AccountsPayable />
              </Layout>
            }
          />
        </Route>
      </Route>

      <Route path="/404" element={<NotFound />} />
      <Route path="*" element={<Navigate to="/404" replace />} />
    </Routes>
  );
}
