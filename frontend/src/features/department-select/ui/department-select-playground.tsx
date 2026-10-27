"use client";

import { useState } from "react";
import { ArrowClockwiseIcon, TreeStructureIcon } from "@phosphor-icons/react";

import { Button } from "@/shared/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";
import type { DepartmentSelectOption } from "../model/types";
import { DepartmentSelect } from "./department-select";

const previewStates = [
  { value: "ready", label: "Список" },
  { value: "loading", label: "Загрузка" },
  { value: "error", label: "Ошибка" },
] as const;

export function DepartmentSelectPlayground() {
  const [single, setSingle] = useState<DepartmentSelectOption | null>(null);
  const [multiple, setMultiple] = useState<DepartmentSelectOption[]>([]);
  const [resetKey, setResetKey] = useState(0);

  return (
    <main className="flex min-h-screen flex-col items-center bg-muted/30 px-6 py-10 sm:py-15">
      <div className="w-full max-w-5xl">
        <header className="mb-10">
          <p className="mb-3 text-xs tracking-[0.2em] text-muted-foreground uppercase">
            Playground
          </p>
          <div className="flex items-center gap-4">
            <span className="flex size-12 shrink-0 items-center justify-center bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
              <TreeStructureIcon
                size={26}
                weight="duotone"
                aria-hidden="true"
              />
            </span>
            <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">
              Выбор подразделений
            </h1>
          </div>
          <p className="mt-4 max-w-xl text-sm leading-relaxed text-muted-foreground">
            Одиночный и множественный выбор подразделений. Поиск и фильтры
            каждого поля работают независимо.
          </p>
        </header>

        <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
          <div
            className="flex flex-wrap items-center gap-1"
            role="group"
            aria-label="Состояние списка"
          >
            <span className="mr-2 text-xs text-muted-foreground">
              Состояние списка
            </span>
            {previewStates.map((state) => (
              <Button key={state.value} type="button" size="sm">
                {state.label}
              </Button>
            ))}
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => {
              setSingle(null);
              setMultiple([]);
              setResetKey((key) => key + 1);
            }}
          >
            <ArrowClockwiseIcon aria-hidden="true" />
            Сбросить примеры
          </Button>
        </div>

        <div key={resetKey} className="grid items-start gap-6 lg:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>Одно подразделение</CardTitle>
              <CardDescription>
                Например, новый родитель при переносе подразделения.
              </CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4">
              <DepartmentSelect
                mode="single"
                label="Родительское подразделение"
                value={single}
                onChange={setSingle}
              />
              <p className="text-xs leading-relaxed text-muted-foreground">
                Новый выбор заменяет предыдущий.
              </p>
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>Несколько подразделений</CardTitle>
              <CardDescription>
                Например, подразделения в фильтре локаций.
              </CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4">
              <DepartmentSelect
                mode="multiple"
                value={multiple}
                onChange={setMultiple}
                placeholder="Выберите подразделения"
              />
              <p className="text-xs leading-relaxed text-muted-foreground">
                Отметьте несколько подразделений. Чтобы убрать выбранное,
                нажмите крестик на его badge.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </main>
  );
}
