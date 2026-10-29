"use client";

import { positionsApi, positionsQueryOptions } from "@/entities/position";
import { ApiRequestError } from "@/shared/api";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useDeletePosition() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: positionsApi.deletePositionAsync,
    onSettled: async () => {
      await queryClient.invalidateQueries({
        queryKey: [positionsQueryOptions.baseKey],
      });
    },
    onSuccess: () => {
      toast.success("Должность была успешно удалена!");
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

      toast.error("Не удалось удалить должность");
    },
  });

  return {
    deletePositionFunc: mutation.mutate,
    isError: mutation.isError,
    error: mutation.error,
    isPending: mutation.isPending,
  };
}
