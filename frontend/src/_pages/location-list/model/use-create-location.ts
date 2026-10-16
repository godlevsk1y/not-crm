"use client";

import { locationsApi } from "@/entities/location";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import z from "zod";

export const createLocationSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Укажите название локации")
    .max(100, "Максимум 100 символов"),

  country: z.string().trim().min(1, "Укажите страну"),
  region: z.string().trim(),

  city: z.string().trim().min(1, "Укажите город"),
  district: z.string().trim(),

  street: z.string().trim().min(1, "Укажите улицу"),
  houseNumber: z.string().trim().min(1, "Укажите номер дома"),

  postalCode: z.string().trim(),
});

export type CreateLocationFormValues = z.infer<typeof createLocationSchema>;

export function useCreateLocation() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: locationsApi.createLocationAsync,
    onSettled: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["locations"],
      });
    },
    onSuccess: () => {
      toast.success("Локация была успешно создана!");
    },
    onError: () => {
      toast.error("Не удалось создать локацию");
    },
  });

  return {
    createLocationFunc: mutation.mutate,
    isError: mutation.isError,
    error: mutation.error,
    isPending: mutation.isPending,
  };
}
