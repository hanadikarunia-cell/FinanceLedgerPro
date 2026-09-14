import { axiosClient } from './axiosClient';
import type { ExportFormat, ReportFilters } from '@/types';

const EXTENSIONS: Record<ExportFormat, string> = {
  excel: 'xlsx',
  pdf: 'pdf',
  csv: 'csv',
};

function triggerBlobDownload(blob: Blob, fileName: string) {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export const exportApi = {
  async download(
    format: ExportFormat,
    filters: ReportFilters = {},
    fileNameBase = 'finance-report',
  ): Promise<void> {
    const { data } = await axiosClient.post(`/export/${format}`, filters, {
      responseType: 'blob',
    });
    const stamp = new Date().toISOString().slice(0, 10);
    triggerBlobDownload(data as Blob, `${fileNameBase}-${stamp}.${EXTENSIONS[format]}`);
  },
};
