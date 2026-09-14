import { axiosClient } from './axiosClient';
import type { Attachment } from '@/types';

export const filesApi = {
  async upload(
    file: File,
    onProgress?: (percent: number) => void,
  ): Promise<Attachment> {
    const form = new FormData();
    form.append('file', file);
    const { data } = await axiosClient.post<Attachment>('/files/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: (evt) => {
        if (onProgress && evt.total) {
          onProgress(Math.round((evt.loaded * 100) / evt.total));
        }
      },
    });
    return data;
  },
};
