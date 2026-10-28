"use client";

import { useState } from "react";
import { TreeStructureIcon } from "@phosphor-icons/react";

import type { DepartmentNodeDto } from "@/entities/department";
import { Card, CardContent } from "@/shared/ui/card";

import { DepartmentDetails } from "./department-details";
import { DepartmentPositions } from "./department-positions";
import { DepartmentTree } from "./department-tree";

export function OrganizationStructureSection() {
  const [selectedDepartment, setSelectedDepartment] =
    useState<DepartmentNodeDto | null>(null);

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
            selectedId={selectedDepartment?.id}
            onSelect={setSelectedDepartment}
          />
        </nav>
      </Card>

      <section aria-label="Выбранное подразделение" className="min-w-0">
        {selectedDepartment ? (
          <div className="space-y-8">
            <DepartmentDetails department={selectedDepartment} />
            <DepartmentPositions
              key={selectedDepartment.id}
              id={selectedDepartment.id}
            />
          </div>
        ) : (
          <Card className="shadow-sm [--card-spacing:--spacing(6)]">
            <CardContent
              role="status"
              className="flex flex-col items-center py-10 text-center sm:py-16"
            >
              <span className="mb-6 flex size-14 items-center justify-center bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
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
