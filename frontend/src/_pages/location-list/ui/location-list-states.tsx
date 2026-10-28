"use client";

import {
  ArrowClockwiseIcon,
  MapPinIcon,
  WarningCircleIcon,
} from "@phosphor-icons/react";

import { LocationListItemCardSkeleton } from "@/entities/location";

import { Button } from "@/shared/ui/button";
import { Card, CardContent } from "@/shared/ui/card";

export function LocationListLoading() {
  return (
    <div role="status">
      <p className="sr-only">Загружаем локации…</p>

      <div
        className="grid gap-5 md:grid-cols-2 xl:grid-cols-3"
        aria-hidden="true"
      >
        {Array.from({ length: 6 }, (_, index) => (
          <LocationListItemCardSkeleton key={index} />
        ))}
      </div>
    </div>
  );
}

type LocationListErrorProps = {
  message: string;
  onRetry: () => void;
};

export function LocationListError({
  message,
  onRetry,
}: LocationListErrorProps) {
  return (
    <Card className="shadow-sm [--card-spacing:--spacing(6)]">
      <CardContent className="flex flex-col items-center py-10 text-center sm:py-16">
        <div role="alert" className="flex max-w-sm flex-col items-center">
          <span className="mb-6 flex size-14 items-center justify-center bg-destructive/10 text-destructive">
            <WarningCircleIcon size={30} weight="duotone" aria-hidden="true" />
          </span>

          <h3 className="text-lg font-semibold">
            Не удалось загрузить локации
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

export function LocationListEmpty({
  isFiltered = false,
}: {
  isFiltered?: boolean;
}) {
  return (
    <Card className="shadow-sm [--card-spacing:--spacing(6)]">
      <CardContent
        role="status"
        className="flex flex-col items-center py-10 text-center sm:py-16"
      >
        <span className="mb-6 flex size-14 items-center justify-center bg-sky-500/10 text-sky-600 dark:text-sky-400">
          <MapPinIcon size={30} weight="duotone" aria-hidden="true" />
        </span>

        <h3 className="text-lg font-semibold">
          {isFiltered ? "Локации не найдены" : "Локаций пока нет"}
        </h3>

        <p className="mt-3 max-w-sm text-sm leading-relaxed text-muted-foreground">
          {isFiltered
            ? "С выбранными подразделениями не связано ни одной локации. Измените или очистите фильтр."
            : "Здесь появятся адреса офисов и рабочих пространств, когда в справочник добавят первую локацию."}
        </p>
      </CardContent>
    </Card>
  );
}
