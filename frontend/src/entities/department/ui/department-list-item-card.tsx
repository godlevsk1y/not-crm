import {
  PencilSimpleIcon,
  TrashIcon,
  TreeStructureIcon,
} from "@phosphor-icons/react/ssr";
import { Button } from "@/shared/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";
import type { DepartmentListItemDto } from "../model/types";

const dateFormatter = new Intl.DateTimeFormat("ru-RU", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});

export function DepartmentListItemCard({
  department,
  onUpdate,
  onDelete,
}: {
  department: DepartmentListItemDto;
  onUpdate: () => void;
  onDelete: () => void;
}) {
  const { name, slug, path, createdAt } = department;
  const createdAtDate = new Date(createdAt);

  return (
    <Card className="h-full shadow-sm transition duration-200 hover:shadow-lg hover:ring-foreground/20 motion-safe:hover:-translate-y-1 [--card-spacing:--spacing(6)]">
      <CardHeader className="gap-3">
        <div className="mb-2 flex items-center justify-between gap-3">
          <span className="flex size-10 shrink-0 items-center justify-center bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
            <TreeStructureIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <span className="min-w-0 wrap-break-words bg-muted px-3 py-1 font-mono text-xs text-muted-foreground">
            {slug}
          </span>
        </div>
        <CardTitle className="text-lg font-semibold">
          <h3 className="wrap-break-words">{name}</h3>
        </CardTitle>
      </CardHeader>

      <CardContent className="flex-1">
        <p className="text-xs text-muted-foreground">Путь в структуре</p>
        <p className="mt-2 wrap-break-words font-mono text-sm leading-relaxed">
          {path}
        </p>
      </CardContent>

      <CardFooter className="mt-auto flex-col items-start gap-3">
        <p className="text-xs text-muted-foreground">
          Добавлено{" "}
          <time dateTime={createdAtDate.toISOString()}>
            {dateFormatter.format(createdAtDate)}
          </time>
        </p>
        <div className="flex w-full flex-wrap gap-2">
          <Button type="button" variant="outline" size="sm" onClick={onUpdate}>
            <PencilSimpleIcon aria-hidden="true" />
            Редактировать
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
        </div>
      </CardFooter>
    </Card>
  );
}
