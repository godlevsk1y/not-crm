import { ArrowClockwiseIcon, WarningCircleIcon } from "@phosphor-icons/react";

import { Button } from "@/shared/ui/button";
import { Spinner } from "@/shared/ui/spinner";

export function QueryError({
  message,
  onRetry,
}: {
  message: string;
  onRetry: () => void;
}) {
  return (
    <div role="alert" className="space-y-3 p-3">
      <p className="flex items-start gap-2 text-xs text-destructive">
        <WarningCircleIcon className="size-4 shrink-0" aria-hidden="true" />
        {message}
      </p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        <ArrowClockwiseIcon aria-hidden="true" />
        Попробовать снова
      </Button>
    </div>
  );
}

export function LoadMore({
  isFetching,
  onLoadMore,
}: {
  isFetching: boolean;
  onLoadMore: () => void;
}) {
  return (
    <Button
      variant="outline"
      size="sm"
      disabled={isFetching}
      onClick={onLoadMore}
      className="my-2"
    >
      {isFetching && <Spinner aria-hidden="true" />}
      {isFetching ? "Загружаем…" : "Загрузить ещё"}
    </Button>
  );
}
