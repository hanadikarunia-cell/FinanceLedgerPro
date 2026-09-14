import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Grid from '@mui/material/Grid';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import type { GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import DataTable from '@/components/DataTable';
import { useAuditLogs } from '@/hooks/useAuditLogs';
import type { AuditLog, AuditLogFilters } from '@/types';
import { formatDateTime, getErrorMessage } from '@/utils/format';

export default function AuditLogs() {
  const { t } = useTranslation();
  const [filters, setFilters] = useState<AuditLogFilters>({ page: 1, pageSize: 25 });
  const { data, isLoading, isFetching, isError, error } = useAuditLogs(filters);

  const paginationModel: GridPaginationModel = {
    page: (filters.page ?? 1) - 1,
    pageSize: filters.pageSize ?? 25,
  };

  const columns: GridColDef<AuditLog>[] = [
    {
      field: 'timestamp',
      headerName: t('auditLogs.timestamp'),
      width: 200,
      valueFormatter: (v) => formatDateTime(v as string),
    },
    { field: 'userName', headerName: t('common.user'), width: 160 },
    { field: 'action', headerName: t('auditLogs.action'), width: 140 },
    { field: 'entity', headerName: t('auditLogs.entity'), width: 140 },
    { field: 'entityId', headerName: t('auditLogs.entityId'), width: 160 },
    { field: 'details', headerName: t('auditLogs.details'), flex: 1, minWidth: 200 },
    { field: 'ipAddress', headerName: t('auditLogs.ip'), width: 140 },
  ];

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        {t('auditLogs.title')}
      </Typography>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                label={t('auditLogs.action')}
                fullWidth
                size="small"
                value={filters.action ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, action: e.target.value || undefined, page: 1 }))
                }
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                label={t('auditLogs.entity')}
                fullWidth
                size="small"
                value={filters.entity ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, entity: e.target.value || undefined, page: 1 }))
                }
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                label={t('common.from')}
                type="date"
                fullWidth
                size="small"
                InputLabelProps={{ shrink: true }}
                value={filters.from ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, from: e.target.value || undefined, page: 1 }))
                }
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                label={t('common.to')}
                type="date"
                fullWidth
                size="small"
                InputLabelProps={{ shrink: true }}
                value={filters.to ?? ''}
                onChange={(e) =>
                  setFilters((f) => ({ ...f, to: e.target.value || undefined, page: 1 }))
                }
              />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getErrorMessage(error, t('auditLogs.failedToLoad'))}
        </Alert>
      )}

      <Card>
        <CardContent>
          <DataTable
            rows={data?.items ?? []}
            columns={columns}
            loading={isLoading || isFetching}
            getRowId={(row) => row.id}
            paginationMode="server"
            rowCount={data?.total ?? 0}
            paginationModel={paginationModel}
            onPaginationModelChange={(model) =>
              setFilters((prev) => ({
                ...prev,
                page: model.page + 1,
                pageSize: model.pageSize,
              }))
            }
          />
        </CardContent>
      </Card>
    </Box>
  );
}
