"use client";

import {
  type PositionFormValues,
  type PositionListItemDto,
  positionSchema,
} from "@/entities/position";
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
import { BriefcaseIcon } from "@phosphor-icons/react";
import { useId } from "react";
import { type FieldPath, useForm } from "react-hook-form";
import { useUpdatePosition } from "../model/use-update-position";

const fieldNameMap = new Map<string, FieldPath<PositionFormValues>>([
  ["PositionName", "name"],
  ["Name", "name"],
]);

const errorCodeMessagesMap = new Map<string, string>([
  ["position.name.empty", "Укажите название должности"],
  ["position.name.long", "Максимум 100 символов"],
]);

export function UpdatePositionDialog({
  position,
  onClose,
}: {
  position: PositionListItemDto;
  onClose: () => void;
}) {
  const id = useId();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid },
  } = useForm<PositionFormValues>({
    resolver: zodResolver(positionSchema),
    mode: "onTouched",
    defaultValues: { name: position.name },
  });
  const { updatePositionFunc, isPending } = useUpdatePosition();

  const onSubmit = (values: PositionFormValues) => {
    if (isPending) return;

    updatePositionFunc(
      { id: position.id, ...values },
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
          <span className="flex size-10 shrink-0 items-center justify-center bg-violet-500/10 text-violet-600 dark:text-violet-400">
            <BriefcaseIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <DialogTitle className="text-xl font-semibold tracking-tight">
            Редактировать должность
          </DialogTitle>
          <DialogDescription>Уточните название должности.</DialogDescription>
        </DialogHeader>

        <form className="grid gap-6" onSubmit={handleSubmit(onSubmit)}>
          <div className="grid gap-2">
            <label htmlFor={`${id}-name`} className="font-medium">
              Название должности
            </label>
            <Input
              id={`${id}-name`}
              placeholder="Например, Разработчик"
              {...register("name")}
              aria-invalid={Boolean(errors.name)}
              aria-describedby={errors.name ? `${id}-name-error` : undefined}
            />
            <p
              id={`${id}-name-error`}
              role={errors.name ? "alert" : undefined}
              className="h-4 text-xs leading-4 text-destructive"
            >
              {errors.name?.message}
            </p>
          </div>

          <DialogFooter className="border-t pt-4">
            <DialogClose render={<Button type="button" variant="outline" />}>
              Отмена
            </DialogClose>
            <Button type="submit" disabled={isPending || !isValid}>
              Обновить должность
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
