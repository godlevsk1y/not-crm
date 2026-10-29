"use client";

import { departmentsApi, departmentsQueryOptions } from "@/entities/department";
import { ApiRequestError } from "@/shared/api";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

const errorMessagesByCode = new Map<string, string>([
  ["department.not.found", "Департамент не найден"],
  [
    "department.has.active.children",
    "Нельзя удалить департамент, у которого есть активные дочерние департаменты",
  ],
]);

function getErrorMessage(error: unknown) {
  const message =
    error instanceof ApiRequestError
      ? error.messages
          .map((message) => errorMessagesByCode.get(message.code))
          .find((message) => message !== undefined)
      : undefined;

  return message ?? "Не удалось удалить департамент";
}

export function useDeleteDepartment() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: departmentsApi.deleteDepartmentAsync,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: [departmentsQueryOptions.baseKey],
      });
      toast.success("Департамент был успешно удалён!");
    },
    onError: (error) => {
      toast.error(getErrorMessage(error));
    },
  });

  return {
    deleteDepartmentFunc: mutation.mutate,
    isError: mutation.isError,
    error: mutation.error,
    errorMessage: mutation.isError ? getErrorMessage(mutation.error) : undefined,
    isPending: mutation.isPending,
  };
}
