"use client";

import { useState } from "react";
import { TreeStructureIcon } from "@phosphor-icons/react";

import {
  UpdateDepartmentDialog,
  DeleteDepartmentDialog,
  TransferDepartmentDialog,
} from "@/features/department-actions";
import { Card, CardContent } from "@/shared/ui/card";
import { ApiRequestError } from "@/shared/api";

import { useSelectedDepartment } from "../model/use-selected-department";
import { DepartmentDetails } from "./department-details";
import { DepartmentPositions } from "./department-positions";
import { DepartmentTree } from "./department-tree";
import { QueryError } from "./query-controls";

export function OrganizationStructureSection() {
  const {
    selectedId,
    selectDepartment,
    branch,
    department: currentDepartment,
    isInvalidId,
    isPending,
    isError,
    error,
    refetch,
  } = useSelectedDepartment();
  const [action, setAction] = useState<{
    id: string;
    type: "update" | "delete" | "transfer";
  } | null>(null);
  const isNotFound = error instanceof ApiRequestError && error.status === 404;

  return (
    <div className="grid items-start gap-6 md:grid-cols-[minmax(0,18rem)_minmax(0,1fr)]">
      <Card className="min-w-0 gap-0 py-0 shadow-sm">
        <div className="border-b px-5 py-4">
          <h2 className="text-sm font-medium">Подразделения</h2>
          <p className="mt-1 text-xs text-muted-foreground">
            Структура компании
          </p>
        </div>
        <nav
          aria-label="Дерево подразделений"
          className="max-h-[60vh] overflow-auto p-2 md:max-h-[70vh]"
        >
          <DepartmentTree
            selectedId={selectedId}
            branch={branch}
            onSelect={(department) => selectDepartment(department.id)}
          />
        </nav>
      </Card>

      <section aria-label="Выбранное подразделение" className="min-w-0">
        {isInvalidId ? (
          <p role="alert">Некорректный ID подразделения в ссылке.</p>
        ) : isPending ? (
          <p role="status">Загружаем подразделение…</p>
        ) : isNotFound ? (
          <p role="alert">Подразделение не найдено или было удалено.</p>
        ) : isError ? (
          <QueryError
            message="Не удалось загрузить подразделение."
            onRetry={() => void refetch()}
          />
        ) : currentDepartment ? (
          <div className="space-y-8">
            <DepartmentDetails
              department={currentDepartment}
              onUpdate={() =>
                setAction({ id: currentDepartment.id, type: "update" })
              }
              onDelete={() =>
                setAction({ id: currentDepartment.id, type: "delete" })
              }
              onTransfer={() =>
                setAction({ id: currentDepartment.id, type: "transfer" })
              }
            />
            <DepartmentPositions
              key={currentDepartment.id}
              id={currentDepartment.id}
            />
            {action?.id === currentDepartment.id && action.type === "update" && (
              <UpdateDepartmentDialog
                department={currentDepartment}
                onClose={() => setAction(null)}
              />
            )}
            {action?.id === currentDepartment.id && action.type === "delete" && (
              <DeleteDepartmentDialog
                department={currentDepartment}
                onClose={() => setAction(null)}
                onDeleted={() => selectDepartment(undefined, true)}
              />
            )}
            {action?.id === currentDepartment.id && action.type === "transfer" && (
              <TransferDepartmentDialog
                key={`transfer-${currentDepartment.id}`}
                department={currentDepartment}
                onClose={() => setAction(null)}
              />
            )}
          </div>
        ) : (
          <Card className="shadow-sm [--card-spacing:--spacing(6)]">
            <CardContent
              role="status"
              className="flex flex-col items-center py-10 text-center sm:py-16"
            >
              <span className="mb-6 flex size-14 items-center justify-center bg-yellow-500/10 text-yellow-700 dark:text-yellow-400">
                <TreeStructureIcon
                  size={30}
                  weight="duotone"
                  aria-hidden="true"
                />
              </span>
              <h2 className="text-lg font-semibold">Выберите подразделение</h2>
              <p className="mt-3 max-w-sm text-sm leading-relaxed text-muted-foreground">
                Здесь появятся информация о подразделении и его должности.
                Раскройте папки в дереве, чтобы увидеть вложенные подразделения.
              </p>
            </CardContent>
          </Card>
        )}
      </section>
    </div>
  );
}
