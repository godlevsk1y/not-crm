"use client";

import { positionsApi, positionsQueryOptions } from "@/entities/position";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useCreatePosition() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: positionsApi.createPositionAsync,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: [positionsQueryOptions.baseKey],
      });
      toast.success("Должность была успешно создана!");
    },
    onError: () => {
      toast.error("Не удалось создать должность");
    },
  });

  return {
    createPositionFunc: mutation.mutate,
    isPending: mutation.isPending,
  };
}
