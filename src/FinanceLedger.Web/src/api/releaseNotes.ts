import { axiosClient } from './axiosClient';
import type { ReleaseNote, ReleaseNoteInput } from '@/types';

export const releaseNotesApi = {
  async list(): Promise<ReleaseNote[]> {
    const { data } = await axiosClient.get<ReleaseNote[]>('/release-notes');
    return data;
  },

  async create(payload: ReleaseNoteInput): Promise<ReleaseNote> {
    const { data } = await axiosClient.post<ReleaseNote>('/release-notes', payload);
    return data;
  },

  async update(id: string, payload: ReleaseNoteInput): Promise<ReleaseNote> {
    const { data } = await axiosClient.put<ReleaseNote>(`/release-notes/${id}`, payload);
    return data;
  },

  async remove(id: string): Promise<void> {
    await axiosClient.delete(`/release-notes/${id}`);
  },
};
