"use client";

import {
  type DepartmentFormValues,
  type DepartmentListItemDto,
  departmentSchema,
} from "@/entities/department";
import { ApiRequestError } from "@/shared/api";
import { applyServerErrors } from "@/shared/lib/react-hook-form";
import { Button } from "@/shared/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { zodResolver } from "@hookform/resolvers/zod";
import { TreeStructureIcon } from "@phosphor-icons/react";
import { useId } from "react";
import { type FieldPath, useForm } from "react-hook-form";
import { useUpdateDepartment } from "../model/use-update-department";

const fieldNameMap = new Map<string, FieldPath<DepartmentFormValues>>([
  ["Name", "name"],
  ["DepartmentName", "name"],
  ["Slug", "slug"],
]);

const errorCodeMessagesMap = new Map<string, string>([
  ["department.name.empty", "Укажите название департамента"],
  ["department.name.long", "Название департамента слишком длинное"],
  ["slug.empty", "Укажите slug"],
  [
    "slug.invalid",
    "Используйте строчные латинские буквы, цифры и дефисы. Дефис не может быть в начале или конце",
  ],
]);

export function UpdateDepartmentDialog({
  department,
  onClose,
}: {
  department: DepartmentListItemDto;
  onClose: () => void;
}) {
  const id = useId();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid },
  } = useForm<DepartmentFormValues>({
    resolver: zodResolver(departmentSchema),
    mode: "onTouched",
    defaultValues: {
      name: department.name,
      slug: department.slug,
    },
  });

  const { updateDepartmentFunc, isPending } = useUpdateDepartment();

  const onSubmit = (values: DepartmentFormValues) => {
    if (isPending) return;

    updateDepartmentFunc(
      { id: department.id, ...values },
      {
        onSuccess: () => onClose(),
        onError: (error) => {
          if (error instanceof ApiRequestError) {
            applyServerErrors({
              setErrors: setError,
              error,
              fieldNameMap,
              errorCodeMessagesMap,
            });

            // Slug value-object errors have no invalidField in the API response.
            for (const message of error.messages) {
              if (
                message.code === "slug.empty" ||
                message.code === "slug.invalid"
              ) {
                setError("slug", {
                  type: error.type,
                  message: errorCodeMessagesMap.get(message.code),
                });
              }
            }
          }
        },
      },
    );
  };

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent className="max-h-[calc(100dvh-2rem)] gap-6 overflow-y-auto p-6 sm:max-w-xl">
        <DialogHeader className="gap-3 pr-6">
          <span className="flex size-10 shrink-0 items-center justify-center bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
            <TreeStructureIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <DialogTitle className="text-xl font-semibold tracking-tight">
            Редактировать департамент
          </DialogTitle>
          <DialogDescription>
            Уточните название и slug департамента.
          </DialogDescription>
        </DialogHeader>

        <form className="grid gap-6" onSubmit={handleSubmit(onSubmit)}>
          <div className="grid gap-2">
            <label htmlFor={`${id}-name`} className="font-medium">
              Название департамента
            </label>
            <Input
              id={`${id}-name`}
              placeholder="Например, Разработка"
              {...register("name")}
              aria-invalid={Boolean(errors.name)}
              aria-describedby={errors.name ? `${id}-name-error` : undefined}
            />
            <p
              id={`${id}-name-error`}
              role={errors.name ? "alert" : undefined}
              className="min-h-4 text-xs leading-4 text-destructive"
            >
              {errors.name?.message}
            </p>
          </div>

          <div className="grid gap-2">
            <label htmlFor={`${id}-slug`} className="font-medium">
              Slug
            </label>
            <Input
              id={`${id}-slug`}
              placeholder="Например, development"
              {...register("slug")}
              aria-invalid={Boolean(errors.slug)}
              aria-describedby={errors.slug ? `${id}-slug-error` : undefined}
            />
            <p
              id={`${id}-slug-error`}
              role={errors.slug ? "alert" : undefined}
              className="min-h-4 text-xs leading-4 text-destructive"
            >
              {errors.slug?.message}
            </p>
          </div>

          <DialogFooter className="border-t pt-4">
            <DialogClose render={<Button type="button" variant="outline" />}>
              Отмена
            </DialogClose>
            <Button type="submit" disabled={isPending || !isValid}>
              Обновить департамент
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
