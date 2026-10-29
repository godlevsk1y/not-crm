"use client";

import { departmentsApi, departmentsQueryOptions } from "@/entities/department";
import { ApiRequestError } from "@/shared/api";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

const errorMessagesByCode = new Map<string, string>([
  ["department.not.found", "Департамент не найден"],
  ["department.name.empty", "Укажите название департамента"],
  ["department.name.long", "Название департамента слишком длинное"],
  ["slug.empty", "Укажите slug"],
  ["slug.invalid", "Проверьте формат slug"],
]);

export function useUpdateDepartment() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: departmentsApi.updateDepartmentAsync,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: [departmentsQueryOptions.baseKey],
      });
      toast.success("Департамент был успешно обновлён!");
    },
    onError: (error) => {
      const message =
        error instanceof ApiRequestError
          ? error.messages
              .map((message) => errorMessagesByCode.get(message.code))
              .find((message) => message !== undefined)
          : undefined;

      toast.error(message ?? "Не удалось обновить департамент");
    },
  });

  return {
    updateDepartmentFunc: mutation.mutate,
    isError: mutation.isError,
    error: mutation.error,
    isPending: mutation.isPending,
  };
}
