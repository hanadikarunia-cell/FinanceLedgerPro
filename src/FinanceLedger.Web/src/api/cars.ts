import { axiosClient } from './axiosClient';
import type { Car, CarInput } from '@/types';

export const carsApi = {
  async list(): Promise<Car[]> {
    const { data } = await axiosClient.get<Car[]>('/cars');
    return data;
  },

  async create(payload: CarInput): Promise<Car> {
    const { data } = await axiosClient.post<Car>('/cars', payload);
    return data;
  },

  async update(id: string, payload: CarInput): Promise<Car> {
    const { data } = await axiosClient.put<Car>(`/cars/${id}`, payload);
    return data;
  },
};
