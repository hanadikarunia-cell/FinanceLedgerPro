import Chip from '@mui/material/Chip';
import { useTranslation } from 'react-i18next';
import type { ApprovalStatus, TransactionType } from '@/types';

const STATUS_COLOR: Record<
  ApprovalStatus,
  'default' | 'info' | 'success' | 'error' | 'warning'
> = {
  Draft: 'default',
  Submitted: 'info',
  Approved: 'success',
  Rejected: 'error',
};

export function StatusChip({ status }: { status: ApprovalStatus }) {
  const { t } = useTranslation();
  return (
    <Chip
      label={t(`enums.status.${status}`)}
      color={STATUS_COLOR[status]}
      size="small"
      variant="filled"
    />
  );
}

export function TypeChip({ type }: { type: TransactionType }) {
  const { t } = useTranslation();
  return (
    <Chip
      label={t(`enums.type.${type}`)}
      color={type === 'Income' ? 'success' : 'warning'}
      size="small"
      variant="outlined"
    />
  );
}
