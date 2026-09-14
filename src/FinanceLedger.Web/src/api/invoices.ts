import { axiosClient } from './axiosClient';
import type { CreateInvoiceInput, Invoice, InvoiceFilters, PagedResult } from '@/types';

export const invoicesApi = {
  async list(filters: InvoiceFilters = {}): Promise<PagedResult<Invoice>> {
    const { data } = await axiosClient.get<PagedResult<Invoice>>('/invoices', {
      params: filters,
    });
    return data;
  },

  async create(payload: CreateInvoiceInput): Promise<Invoice> {
    const { data } = await axiosClient.post<Invoice>('/invoices', payload);
    return data;
  },

  async markPaid(id: string): Promise<Invoice> {
    const { data } = await axiosClient.post<Invoice>(`/invoices/${id}/mark-paid`);
    return data;
  },

  async void(id: string): Promise<Invoice> {
    const { data } = await axiosClient.post<Invoice>(`/invoices/${id}/void`);
    return data;
  },

  async remove(id: string): Promise<void> {
    await axiosClient.delete(`/invoices/${id}`);
  },
};
