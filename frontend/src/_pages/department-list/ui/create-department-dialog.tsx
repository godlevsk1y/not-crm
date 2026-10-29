"use client";

import { DepartmentSelect } from "@/features/department-select";
import { LocationSelect } from "@/features/location-select";
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
  DialogTrigger,
} from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { zodResolver } from "@hookform/resolvers/zod";
import { TreeStructureIcon, PlusIcon } from "@phosphor-icons/react";
import { useId, useState } from "react";
import { Controller, type FieldPath, useForm } from "react-hook-form";
import {
  createDepartmentSchema,
  type CreateDepartmentFormValues,
} from "../model/create-department-schema";
import { useCreateDepartment } from "../model/use-create-department";

const fieldNameMap = new Map<string, FieldPath<CreateDepartmentFormValues>>([
  ["Name", "name"],
  ["DepartmentName", "name"],
  ["Slug", "slug"],
  ["ParentId", "parent"],
  ["LocationIds", "locations"],
]);

const errorCodeMessagesMap = new Map<string, string>([
  ["department.name.empty", "Укажите название департамента"],
  ["department.name.long", "Название подразделения слишком длинное"],
  ["department.locations.null", "Выберите локации или оставьте список пустым"],
  ["department.not.found", "Родительское подразделение не найдено"],
  ["location.not.found", "Одна из выбранных локаций не найдена"],
  ["slug.empty", "Укажите slug"],
  [
    "slug.invalid",
    "Используйте строчные латинские буквы, цифры и дефисы. Дефис не может быть в начале или конце",
  ],
]);

export function CreateDepartmentDialog() {
  const id = useId();
  const [open, setOpen] = useState(false);
  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isValid },
  } = useForm<CreateDepartmentFormValues>({
    resolver: zodResolver(createDepartmentSchema),
    mode: "onTouched",
    defaultValues: {
      name: "",
      slug: "",
      parent: null,
      locations: [],
    },
  });

  const { createDepartmentFunc, isPending } = useCreateDepartment();

  const onSubmit = (values: CreateDepartmentFormValues) => {
    if (isPending) return;

    createDepartmentFunc(
      {
        name: values.name,
        slug: values.slug,
        parentId: values.parent?.id ?? null,
        locationIds: [
          ...new Set(values.locations.map((location) => location.id)),
        ],
      },
      {
        onSuccess: () => {
          reset();
          setOpen(false);
        },
        onError: (error) => {
          if (error instanceof ApiRequestError) {
            applyServerErrors({
              setErrors: setError,
              error,
              fieldNameMap,
              errorCodeMessagesMap,
            });

            // Some API errors identify the field only through their error code.
            for (const message of error.messages) {
              const field = message.code.startsWith("slug.")
                ? "slug"
                : message.code === "department.not.found"
                  ? "parent"
                  : message.code === "location.not.found"
                    ? "locations"
                    : undefined;

              if (field) {
                setError(field, {
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
      open={open}
      onOpenChange={(open) => {
        if (isPending) return;
        setOpen(open);
        if (!open) reset();
      }}
    >
      <DialogTrigger render={<Button size="lg" />}>
        <PlusIcon aria-hidden="true" />
        Добавить подразделение
      </DialogTrigger>

      <DialogContent
        showCloseButton={!isPending}
        className="max-h-[calc(100dvh-2rem)] gap-6 overflow-y-auto p-6 sm:max-w-xl"
      >
        <DialogHeader className="gap-3 pr-6">
          <span className="flex size-10 shrink-0 items-center justify-center bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
            <TreeStructureIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <DialogTitle className="text-xl font-semibold tracking-tight">
            Добавить подразделение
          </DialogTitle>
          <DialogDescription>
            Укажите название и slug. При необходимости выберите родительское
            подразделение и локации.
          </DialogDescription>
        </DialogHeader>

        <form
          className="grid gap-6"
          onSubmit={handleSubmit(onSubmit)}
          aria-busy={isPending}
        >
          <fieldset disabled={isPending} className="grid min-w-0 gap-4">
            <div className="grid gap-2">
              <label htmlFor={`${id}-name`} className="font-medium">
                Название подразделения
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

            <Controller
              name="parent"
              control={control}
              render={({ field }) => (
                <div
                  onBlur={field.onBlur}
                  role="group"
                  aria-label="Родительское подразделение"
                  aria-describedby={
                    errors.parent ? `${id}-parent-error` : undefined
                  }
                >
                  <DepartmentSelect
                    id={`${id}-parent`}
                    mode="single"
                    label="Родительское подразделение (необязательно)"
                    placeholder="Без родительского подразделения"
                    value={field.value}
                    onChange={field.onChange}
                    disabled={isPending}
                  />
                  <p
                    id={`${id}-parent-error`}
                    role={errors.parent ? "alert" : undefined}
                    className="mt-2 min-h-4 text-xs leading-4 text-destructive"
                  >
                    {errors.parent?.message}
                  </p>
                </div>
              )}
            />

            <Controller
              name="locations"
              control={control}
              render={({ field }) => (
                <div
                  onBlur={field.onBlur}
                  role="group"
                  aria-label="Локации"
                  aria-describedby={
                    errors.locations ? `${id}-locations-error` : undefined
                  }
                >
                  <LocationSelect
                    id={`${id}-locations`}
                    mode="multiple"
                    label="Локации (необязательно)"
                    placeholder="Выберите локации"
                    value={field.value}
                    onChange={field.onChange}
                    disabled={isPending}
                  />
                  <p
                    id={`${id}-locations-error`}
                    role={errors.locations ? "alert" : undefined}
                    className="mt-2 min-h-4 text-xs leading-4 text-destructive"
                  >
                    {errors.locations?.message}
                  </p>
                </div>
              )}
            />
          </fieldset>

          <DialogFooter className="border-t pt-4">
            <DialogClose
              render={
                <Button type="button" variant="outline" disabled={isPending} />
              }
            >
              Отмена
            </DialogClose>
            <Button type="submit" disabled={isPending || !isValid}>
              {isPending ? "Добавление…" : "Добавить подразделение"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
