import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { auditLogsApi } from '@/api/auditlogs';
import type { AuditLogFilters } from '@/types';

export function useAuditLogs(filters: AuditLogFilters) {
  return useQuery({
    queryKey: ['auditlogs', filters],
    queryFn: () => auditLogsApi.list(filters),
    placeholderData: keepPreviousData,
  });
}
