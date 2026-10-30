"use client";

import { departmentsApi, departmentsQueryOptions } from "@/entities/department";
import { ApiRequestError } from "@/shared/api";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

const errorMessagesByCode = new Map<string, string>([
  ["department.not.found", "Подразделение не найдено"],
  [
    "department.parent.not.found",
    "Новый родитель не найден. Выберите другое подразделение",
  ],
  [
    "department.transfer.parent_to_self",
    "Подразделение нельзя перенести в само себя",
  ],
  [
    "department.transfer.cycle",
    "Подразделение нельзя перенести в его дочернее подразделение",
  ],
  [
    "department.transfer.conflict",
    "Структура изменилась во время переноса. Попробуйте ещё раз",
  ],
]);

export function useTransferDepartment() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: departmentsApi.transferDepartmentAsync,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: [departmentsQueryOptions.baseKey],
      });
      toast.success("Подразделение перенесено");
    },
  });

  const message =
    mutation.error instanceof ApiRequestError
      ? mutation.error.messages
          .map((message) => errorMessagesByCode.get(message.code))
          .find((message) => message !== undefined)
      : undefined;

  return {
    transferDepartmentFunc: mutation.mutate,
    isPending: mutation.isPending,
    reset: mutation.reset,
    errorMessage: mutation.isError
      ? (message ?? "Не удалось перенести подразделение. Попробуйте ещё раз")
      : undefined,
  };
}
