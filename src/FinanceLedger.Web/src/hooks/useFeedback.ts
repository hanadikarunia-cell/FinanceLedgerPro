import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { feedbackApi } from '@/api/feedback';
import type { FeedbackInput, FeedbackSeverity } from '@/types';

const KEY = 'feedback';

export function useFeedbackList() {
  return useQuery({
    queryKey: [KEY],
    queryFn: () => feedbackApi.list(1, 100),
    select: (data) => data.items,
  });
}

export function useCreateFeedback() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: FeedbackInput) => feedbackApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useSetFeedbackSeverity() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, severity }: { id: string; severity: FeedbackSeverity }) =>
      feedbackApi.setSeverity(id, severity),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}
