import { Navigate, Outlet } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuth } from '@/context/AuthContext';
import type { UserRole } from '@/types';

interface RoleGuardProps {
  /** Roles allowed to view the content. */
  allow: UserRole[];
  /** Optional inline children. When omitted the guard renders an <Outlet /> for route usage. */
  children?: ReactNode;
  /** When true, unauthorized inline usage renders nothing instead of redirecting. */
  hideWhenDenied?: boolean;
  redirectTo?: string;
}

export default function RoleGuard({
  allow,
  children,
  hideWhenDenied,
  redirectTo = '/',
}: RoleGuardProps) {
  const { user } = useAuth();
  const permitted = !!user && allow.includes(user.role);

  if (!permitted) {
    if (children && hideWhenDenied) return null;
    if (children) return <Navigate to={redirectTo} replace />;
    return <Navigate to={redirectTo} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
}
