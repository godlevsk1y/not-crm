"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { TreeStructureIcon } from "@phosphor-icons/react";

import {
  type DepartmentDto,
  departmentsQueryOptions,
} from "@/entities/department";
import {
  DepartmentSelect,
  type DepartmentSelectOption,
} from "@/features/department-select";
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
import { Spinner } from "@/shared/ui/spinner";
import { useTransferDepartment } from "../model/use-transfer-department";

export function TransferDepartmentDialog({
  department,
  onClose,
}: {
  department: Pick<DepartmentDto, "id" | "name">;
  onClose: () => void;
}) {
  const [step, setStep] = useState<"select" | "confirm">("select");
  const [newParent, setNewParent] = useState<DepartmentSelectOption | null>(
    null,
  );
  const details = useQuery({
    ...departmentsQueryOptions.getDepartmentOptions(department.id),
    retry: false,
  });
  const descendants = useQuery({
    ...departmentsQueryOptions.getDepartmentDescendantsOptions(department.id),
    retry: false,
  });
  const parentId = details.data?.parentId;
  const parent = useQuery({
    ...departmentsQueryOptions.getDepartmentOptions(parentId ?? ""),
    enabled: Boolean(parentId),
    retry: false,
  });
  const { transferDepartmentFunc, isPending, errorMessage, reset } =
    useTransferDepartment();

  const hasLoadError =
    details.isError ||
    descendants.isError ||
    (Boolean(parentId) && parent.isError);
  // Wait for the complete server list; the visible tree may contain only part of the subtree.
  const isReady =
    details.isSuccess &&
    descendants.isSuccess &&
    !descendants.isFetching &&
    (parentId === null || parent.isSuccess);

  function handleTransfer() {
    if (isPending || !isReady || step !== "confirm") return;

    transferDepartmentFunc(
      { id: department.id, newParentId: newParent?.id ?? null },
      { onSuccess: onClose },
    );
  }

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open && !isPending) onClose();
      }}
    >
      <DialogContent
        showCloseButton={!isPending}
        className="max-h-[calc(100dvh-2rem)] gap-6 overflow-y-auto p-6 sm:max-w-xl"
      >
        <DialogHeader className="gap-3 pr-6">
          <span className="flex size-10 shrink-0 items-center justify-center bg-yellow-500/10 text-yellow-700 dark:text-yellow-400">
            <TreeStructureIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <DialogTitle className="text-xl font-semibold tracking-tight">
            {step === "select"
              ? "Перенести подразделение"
              : "Подтвердить перенос"}
          </DialogTitle>
          <DialogDescription>
            {step === "select"
              ? "Выберите нового родителя или перенесите подразделение в корень оргструктуры."
              : "Все дочерние подразделения переместятся вместе с выбранным."}
          </DialogDescription>
        </DialogHeader>

        {hasLoadError ? (
          <div role="alert" className="grid gap-3">
            <p className="text-destructive">
              Не удалось загрузить данные для переноса
            </p>
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                if (details.isError) void details.refetch();
                if (descendants.isError) void descendants.refetch();
                if (parentId && parent.isError) void parent.refetch();
              }}
            >
              Повторить
            </Button>
          </div>
        ) : !isReady ? (
          <div
            role="status"
            className="flex items-center gap-2 text-muted-foreground"
          >
            <Spinner aria-hidden="true" /> Загрузка данных для переноса…
          </div>
        ) : step === "select" ? (
          <DepartmentSelect
            mode="single"
            label="Новый родитель"
            value={newParent}
            onChange={setNewParent}
            excludeIds={[
              department.id,
              ...descendants.data.map((item) => item.id),
            ]}
          />
        ) : (
          <dl className="grid gap-4 border bg-muted/30 p-4">
            <div>
              <dt className="text-muted-foreground">Подразделение</dt>
              <dd className="mt-1 wrap-break-words font-semibold">
                {details.data.name}
              </dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Текущий родитель</dt>
              <dd className="mt-1 wrap-break-words">
                {parent.data?.name ?? "Корень оргструктуры"}
              </dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Новый родитель</dt>
              <dd className="mt-1 wrap-break-words">
                {newParent?.name ?? "Корень оргструктуры"}
              </dd>
            </div>
          </dl>
        )}

        {errorMessage && (
          <p role="alert" className="text-destructive">
            {errorMessage}
          </p>
        )}

        <DialogFooter className="flex-wrap border-t pt-4">
          <DialogClose
            render={
              <Button type="button" variant="outline" disabled={isPending} />
            }
          >
            Отмена
          </DialogClose>
          {step === "select" ? (
            <>
              <Button
                type="button"
                variant="outline"
                disabled={!isReady || hasLoadError}
                onClick={() => {
                  setNewParent(null);
                  setStep("confirm");
                }}
              >
                Сделать корневым
              </Button>
              <Button
                type="button"
                disabled={!isReady || hasLoadError || newParent === null}
                onClick={() => setStep("confirm")}
              >
                Далее
              </Button>
            </>
          ) : (
            <>
              <Button
                type="button"
                variant="outline"
                disabled={isPending}
                onClick={() => {
                  reset();
                  setStep("select");
                }}
              >
                Назад
              </Button>
              <Button
                type="button"
                disabled={isPending || !isReady || hasLoadError}
                onClick={handleTransfer}
              >
                {isPending && <Spinner aria-hidden="true" />}
                {isPending ? "Перенос…" : "Перенести"}
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
