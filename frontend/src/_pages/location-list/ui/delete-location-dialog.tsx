"use client";

import type { LocationListItemDto } from "@/entities/location";
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

export function DeleteLocationDialog({
  location,
  onClose,
}: {
  location: LocationListItemDto;
  onClose: () => void;
}) {
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
            Удалить локацию?
          </DialogTitle>
          <DialogDescription>
            Подтвердите удаление выбранной локации.
          </DialogDescription>
        </DialogHeader>

        <div className="min-w-0 border bg-muted/30 p-4">
          <p className="wrap-break-words text-sm font-semibold">
            {location.name}
          </p>
          <p className="mt-1 wrap-break-words text-xs text-muted-foreground">
            {[
              location.address.city,
              location.address.street,
              location.address.houseNumber,
            ].join(", ")}
          </p>
        </div>

        <DialogFooter className="border-t pt-4">
          <DialogClose render={<Button type="button" variant="outline" />}>
            Отмена
          </DialogClose>
          <DialogClose render={<Button type="button" variant="destructive" />}>
            <TrashIcon aria-hidden="true" />
            Удалить
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
