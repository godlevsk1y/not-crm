import {
  PencilSimpleIcon,
  TrashIcon,
  TreeStructureIcon,
} from "@phosphor-icons/react";

import type { DepartmentNodeDto } from "@/entities/department";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardFooter, CardHeader } from "@/shared/ui/card";

const dateFormatter = new Intl.DateTimeFormat("ru-RU", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});

export function DepartmentDetails({
  department,
  onUpdate,
  onDelete,
  onTransfer,
}: {
  department: DepartmentNodeDto;
  onUpdate: () => void;
  onDelete: () => void;
  onTransfer: () => void;
}) {
  const createdAt = new Date(department.createdAt);

  return (
    <Card className="shadow-sm [--card-spacing:--spacing(6)]">
      <CardHeader className="gap-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <span className="flex size-10 shrink-0 items-center justify-center bg-yellow-500/10 text-yellow-700 dark:text-yellow-400">
            <TreeStructureIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <span className="min-w-0 wrap-break-words bg-muted px-3 py-1 font-mono text-xs text-muted-foreground">
            {department.slug}
          </span>
        </div>
        <h2 className="wrap-break-words text-xl font-semibold tracking-tight">
          {department.name}
        </h2>
      </CardHeader>
      <CardContent>
        <dl className="space-y-4">
          <div>
            <dt className="text-xs text-muted-foreground">Путь в структуре</dt>
            <dd className="mt-2 wrap-break-words font-mono text-sm leading-relaxed">
              {department.path}
            </dd>
          </div>
          <div className="flex flex-wrap gap-x-8 gap-y-4 border-t pt-4">
            <div>
              <dt className="text-xs text-muted-foreground">
                Дочерних подразделений
              </dt>
              <dd className="mt-1 text-sm font-medium">
                {department.childCount}
              </dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Добавлено</dt>
              <dd className="mt-1 text-sm">
                <time dateTime={createdAt.toISOString()}>
                  {dateFormatter.format(createdAt)}
                </time>
              </dd>
            </div>
          </div>
        </dl>
      </CardContent>
      <CardFooter className="flex flex-wrap gap-2 border-t pt-4">
        <Button type="button" variant="outline" size="sm" onClick={onUpdate}>
          <PencilSimpleIcon aria-hidden="true" />
          Редактировать
        </Button>
        <Button type="button" variant="outline" size="sm" onClick={onTransfer}>
          <TreeStructureIcon aria-hidden="true" />
          Перенести
        </Button>
        <Button
          type="button"
          variant="destructive"
          size="sm"
          onClick={onDelete}
        >
          <TrashIcon aria-hidden="true" />
          Удалить
        </Button>
      </CardFooter>
    </Card>
  );
}
