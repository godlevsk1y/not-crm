"use client";

import { type PositionFormValues, positionSchema } from "@/entities/position";
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
import { BriefcaseIcon, PlusIcon } from "@phosphor-icons/react";
import { useId, useState } from "react";
import { type FieldPath, useForm } from "react-hook-form";
import { useCreatePosition } from "../model/use-create-position";

const fieldNameMap = new Map<string, FieldPath<PositionFormValues>>([
  ["PositionName", "name"],
  ["Name", "name"],
]);

const errorCodeMessagesMap = new Map<string, string>([
  ["position.name.empty", "Укажите название должности"],
  ["position.name.long", "Максимум 100 символов"],
]);

export function CreatePositionDialog() {
  const id = useId();
  const [open, setOpen] = useState(false);
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isValid },
  } = useForm<PositionFormValues>({
    resolver: zodResolver(positionSchema),
    mode: "onTouched",
    defaultValues: { name: "" },
  });
  const { createPositionFunc, isPending } = useCreatePosition();

  const onSubmit = (values: PositionFormValues) => {
    if (isPending) return;

    createPositionFunc(values, {
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
        }
      },
    });
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
        Добавить должность
      </DialogTrigger>

      <DialogContent
        showCloseButton={!isPending}
        className="max-h-[calc(100dvh-2rem)] gap-6 overflow-y-auto p-6 sm:max-w-xl"
      >
        <DialogHeader className="gap-3 pr-6">
          <span className="flex size-10 shrink-0 items-center justify-center bg-violet-500/10 text-violet-600 dark:text-violet-400">
            <BriefcaseIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <DialogTitle className="text-xl font-semibold tracking-tight">
            Добавить должность
          </DialogTitle>
          <DialogDescription>
            Укажите название новой должности.
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
                className="min-h-4 text-xs leading-4 text-destructive"
              >
                {errors.name?.message}
              </p>
            </div>
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
              {isPending ? "Добавление…" : "Добавить должность"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
