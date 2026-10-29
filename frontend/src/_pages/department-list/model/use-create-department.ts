"use client";

import { departmentsApi, departmentsQueryOptions } from "@/entities/department";
import { locationsQueryOptions } from "@/entities/location";
import { ApiRequestError } from "@/shared/api";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

const errorMessagesByCode = new Map<string, string>([
  ["department.not.found", "Родительское подразделение не найдено"],
  ["location.not.found", "Одна из выбранных локаций не найдена"],
]);

export function useCreateDepartment() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: departmentsApi.createDepartmentAsync,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: [departmentsQueryOptions.baseKey],
      });
      // Location cards also display the number of linked departments.
      void queryClient.invalidateQueries({
        queryKey: [locationsQueryOptions.baseKey],
      });
      toast.success("Подразделение было успешно создано!");
    },
    onError: (error) => {
      const message =
        error instanceof ApiRequestError
          ? error.messages
              .map((message) => errorMessagesByCode.get(message.code))
              .find((message) => message !== undefined)
          : undefined;

      toast.error(message ?? "Не удалось создать подразделение");
    },
  });

  return {
    createDepartmentFunc: mutation.mutate,
    isPending: mutation.isPending,
  };
}
