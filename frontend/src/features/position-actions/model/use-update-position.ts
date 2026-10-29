"use client";

import { positionsApi, positionsQueryOptions } from "@/entities/position";
import { ApiRequestError } from "@/shared/api";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useUpdatePosition() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: positionsApi.updatePositionAsync,
    onSettled: async () => {
      await queryClient.invalidateQueries({
        queryKey: [positionsQueryOptions.baseKey],
      });
    },
    onSuccess: () => {
      toast.success("Должность была успешно обновлена!");
    },
    onError: (error) => {
      if (
        error instanceof ApiRequestError &&
        (error.status === 404 ||
          error.messages.some((message) => message.code === "position.not.found"))
      ) {
        toast.error("Должность не найдена");
        return;
      }

      toast.error("Не удалось обновить должность");
    },
  });

  return {
    updatePositionFunc: mutation.mutate,
    isError: mutation.isError,
    error: mutation.error,
    isPending: mutation.isPending,
  };
}
