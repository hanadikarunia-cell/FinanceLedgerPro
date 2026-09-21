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
import FeedbackPage from '@/pages/Feedback';
import WhatsNew from '@/pages/WhatsNew';
import Sites from '@/pages/Sites';
import NotFound from '@/pages/NotFound';
import { useAuth } from '@/context/AuthContext';

/** The Application Admin has no site data to show, so their home is the Sites list. */
function Home() {
  const { isAppAdmin } = useAuth();
  return isAppAdmin ? <Navigate to="/sites" replace /> : <Dashboard />;
}

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />

      <Route element={<ProtectedRoute />}>
        <Route
          path="/"
          element={
            <Layout>
              <Home />
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
        <Route
          path="/feedback"
          element={
            <Layout>
              <FeedbackPage />
            </Layout>
          }
        />
        <Route
          path="/whats-new"
          element={
            <Layout>
              <WhatsNew />
            </Layout>
          }
        />

        {/* Application Admin only: client sites */}
        <Route element={<RoleGuard allow={['AppAdmin']} />}>
          <Route
            path="/sites"
            element={
              <Layout>
                <Sites />
              </Layout>
            }
          />
        </Route>

        {/* Site-level screens (Site Admin and User). The Application Admin has no site. */}
        <Route element={<RoleGuard allow={['Manager', 'User']} />}>
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
        </Route>

        {/* Site Admin only routes */}
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
