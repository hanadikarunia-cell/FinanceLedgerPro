import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { invoicesApi } from '@/api/invoices';
import type { CreateInvoiceInput, InvoiceFilters } from '@/types';

const KEY = 'invoices';

function invalidateInvoiceDerivedQueries(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: [KEY] });
  qc.invalidateQueries({ queryKey: ['dashboard'] });
  qc.invalidateQueries({ queryKey: ['transactions'] });
  qc.invalidateQueries({ queryKey: ['reports'] });
  qc.invalidateQueries({ queryKey: ['cars'] });
}

export function useInvoices(filters: InvoiceFilters = {}) {
  return useQuery({
    queryKey: [KEY, filters],
    queryFn: () => invoicesApi.list(filters),
  });
}

export function useCreateInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateInvoiceInput) => invoicesApi.create(payload),
    onSuccess: () => invalidateInvoiceDerivedQueries(qc),
  });
}

export function useMarkInvoicePaid() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => invoicesApi.markPaid(id),
    onSuccess: () => invalidateInvoiceDerivedQueries(qc),
  });
}

export function useDeleteInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => invoicesApi.remove(id),
    onSuccess: () => invalidateInvoiceDerivedQueries(qc),
  });
}

export function useVoidInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => invoicesApi.void(id),
    onSuccess: () => invalidateInvoiceDerivedQueries(qc),
  });
}
