import { axiosClient } from './axiosClient';
import type { Feedback, FeedbackInput, FeedbackSeverity, PagedResult } from '@/types';

export const feedbackApi = {
  async list(page = 1, pageSize = 50): Promise<PagedResult<Feedback>> {
    const { data } = await axiosClient.get<PagedResult<Feedback>>('/feedback', {
      params: { page, pageSize },
    });
    return data;
  },

  async create(payload: FeedbackInput): Promise<Feedback> {
    const { data } = await axiosClient.post<Feedback>('/feedback', payload);
    return data;
  },

  async setSeverity(id: string, severity: FeedbackSeverity): Promise<Feedback> {
    const { data } = await axiosClient.put<Feedback>(`/feedback/${id}/severity`, { severity });
    return data;
  },
};
