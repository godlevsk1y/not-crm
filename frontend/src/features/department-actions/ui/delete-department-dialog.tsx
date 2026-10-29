"use client";

import type { DepartmentDto } from "@/entities/department";
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
import { TrashIcon } from "@phosphor-icons/react";
import { useDeleteDepartment } from "../model/use-delete-department";

export function DeleteDepartmentDialog({
  department,
  onClose,
  onDeleted,
}: {
  department: Pick<DepartmentDto, "id" | "name" | "slug">;
  onClose: () => void;
  onDeleted?: () => void;
}) {
  const { deleteDepartmentFunc, isPending, errorMessage } = useDeleteDepartment();

  const handleDelete = () => {
    if (isPending) return;

    deleteDepartmentFunc(
      { id: department.id },
      {
        onSuccess: () => {
          onDeleted?.();
          onClose();
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
      <DialogContent className="max-h-[calc(100dvh-2rem)] gap-6 overflow-y-auto p-6 sm:max-w-md">
        <DialogHeader className="gap-3 pr-6">
          <span className="flex size-10 shrink-0 items-center justify-center bg-destructive/10 text-destructive">
            <TrashIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <DialogTitle className="text-xl font-semibold tracking-tight">
            Удалить департамент?
          </DialogTitle>
          <DialogDescription>
            Подтвердите удаление выбранного департамента.
          </DialogDescription>
        </DialogHeader>

        <div className="min-w-0 border bg-muted/30 p-4">
          <p className="wrap-break-words text-sm font-semibold">
            {department.name}
          </p>
          <p className="mt-1 wrap-break-words font-mono text-xs text-muted-foreground">
            {department.slug}
          </p>
        </div>

        {errorMessage && (
          <p role="alert" className="text-xs leading-relaxed text-destructive">
            {errorMessage}
          </p>
        )}

        <DialogFooter className="border-t pt-4">
          <DialogClose render={<Button type="button" variant="outline" />}>
            Отмена
          </DialogClose>
          <Button
            type="button"
            variant="destructive"
            disabled={isPending}
            onClick={handleDelete}
          >
            <TrashIcon aria-hidden="true" />
            Удалить
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
