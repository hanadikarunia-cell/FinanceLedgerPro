import { axiosClient } from './axiosClient';
import type { AuditLog, AuditLogFilters, PagedResult } from '@/types';

export const auditLogsApi = {
  async list(filters: AuditLogFilters = {}): Promise<PagedResult<AuditLog>> {
    const { data } = await axiosClient.get<PagedResult<AuditLog>>('/auditlogs', {
      params: filters,
    });
    return data;
  },
};
