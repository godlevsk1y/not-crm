"use client";

import { locationsApi } from "@/entities/location";
import { ApiRequestError } from "@/shared/api";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

const errorMessagesByCode = new Map<string, string>([
  ["location.exists", "Локация с таким именем уже существует"],
]);

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
    onError: (error) => {
      if (error instanceof ApiRequestError && error.messages.length == 1) {
        const displayMessage = errorMessagesByCode.get(error.messages[0]?.code);

        if (displayMessage === undefined) {
          toast.error("Не удалось создать локацию");
          return;
        }

        toast.error(displayMessage);
        return;
      }

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
