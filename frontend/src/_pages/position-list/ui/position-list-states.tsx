import {
  ArrowClockwiseIcon,
  BriefcaseIcon,
  WarningCircleIcon,
} from "@phosphor-icons/react";

import { PositionListItemCardSkeleton } from "@/entities/position";

import { Button } from "@/shared/ui/button";
import { Card, CardContent } from "@/shared/ui/card";

export function PositionListLoading({ pageSize }: { pageSize: number }) {
  return (
    <div role="status">
      <p className="sr-only">Загружаем должности…</p>

      <div
        className="grid gap-5 grid-cols-1 md:grid-cols-2 lg:grid-cols-3"
        aria-hidden="true"
      >
        {Array.from({ length: pageSize }, (_, index) => (
          <PositionListItemCardSkeleton key={index} />
        ))}
      </div>
    </div>
  );
}

type PositionListErrorProps = {
  message: string;
  onRetry: () => void;
};

export function PositionListError({
  message,
  onRetry,
}: PositionListErrorProps) {
  return (
    <Card className="shadow-sm [--card-spacing:--spacing(6)]">
      <CardContent className="flex flex-col items-center py-10 text-center sm:py-16">
        <div role="alert" className="flex max-w-sm flex-col items-center">
          <span className="mb-6 flex size-14 items-center justify-center bg-destructive/10 text-destructive">
            <WarningCircleIcon size={30} weight="duotone" aria-hidden="true" />
          </span>

          <h3 className="text-lg font-semibold">
            Не удалось загрузить должности
          </h3>

          <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
            {message}
          </p>
        </div>

        <Button variant="outline" size="lg" className="mt-6" onClick={onRetry}>
          <ArrowClockwiseIcon aria-hidden="true" />
          Попробовать снова
        </Button>
      </CardContent>
    </Card>
  );
}

export function PositionListEmpty({ isActive }: { isActive: boolean }) {
  return (
    <Card className="shadow-sm [--card-spacing:--spacing(6)]">
      <CardContent
        role="status"
        className="flex flex-col items-center py-10 text-center sm:py-16"
      >
        <span className="mb-6 flex size-14 items-center justify-center bg-violet-500/10 text-violet-600 dark:text-violet-400">
          <BriefcaseIcon size={30} weight="duotone" aria-hidden="true" />
        </span>

        <h3 className="text-lg font-semibold">Должности не найдены</h3>

        <p className="mt-3 max-w-sm text-sm leading-relaxed text-muted-foreground">
          {isActive
            ? "Активных должностей нет. Выберите другой статус."
            : "Архивных должностей нет. Выберите другой статус."}
        </p>
      </CardContent>
    </Card>
  );
}
