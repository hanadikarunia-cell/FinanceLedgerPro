import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { releaseNotesApi } from '@/api/releaseNotes';
import type { ReleaseNoteInput } from '@/types';

const KEY = 'release-notes';

export function useReleaseNotes() {
  return useQuery({ queryKey: [KEY], queryFn: () => releaseNotesApi.list() });
}

export function useCreateReleaseNote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: ReleaseNoteInput) => releaseNotesApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateReleaseNote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: ReleaseNoteInput }) =>
      releaseNotesApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDeleteReleaseNote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => releaseNotesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}
