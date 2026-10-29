"use client";

import type { PositionListItemDto } from "@/entities/position";
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
import { useDeletePosition } from "../model/use-delete-position";

export function DeletePositionDialog({
  position,
  onClose,
}: {
  position: PositionListItemDto;
  onClose: () => void;
}) {
  const { deletePositionFunc, isPending } = useDeletePosition();

  const handleDelete = () => {
    if (isPending) return;

    deletePositionFunc(
      { id: position.id },
      { onSuccess: () => onClose() },
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
            Удалить должность?
          </DialogTitle>
          <DialogDescription>
            Подтвердите удаление выбранной должности. Должность также будет удалена
            из всех связанных департаментов.
          </DialogDescription>
        </DialogHeader>

        <div className="min-w-0 border bg-muted/30 p-4">
          <p className="wrap-break-words text-sm font-semibold">
            {position.name}
          </p>
        </div>

        <DialogFooter className="border-t pt-4">
          <DialogClose render={<Button type="button" variant="outline" />}>
            Отмена
          </DialogClose>
          <Button
            type="button"
            variant="destructive"
            onClick={handleDelete}
            disabled={isPending}
          >
            <TrashIcon aria-hidden="true" />
            Удалить
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
