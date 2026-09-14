import { useMemo } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { carsApi } from '@/api/cars';
import type { CarInput } from '@/types';

const KEY = 'cars';

export function useCars() {
  return useQuery({ queryKey: [KEY], queryFn: () => carsApi.list() });
}

export function useCarsLookup() {
  const { data: cars = [], ...rest } = useCars();
  const lookup = useMemo(
    () =>
      cars
        .filter((c) => c.isActive)
        .map((c) => ({ id: c.id, label: `${c.plateNumber} — ${c.model}` })),
    [cars],
  );
  return { data: lookup, ...rest };
}

export function useCreateCar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CarInput) => carsApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useUpdateCar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: CarInput }) =>
      carsApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [KEY] }),
  });
}
