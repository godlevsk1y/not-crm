import { BriefcaseIcon } from "@phosphor-icons/react";

import {
  PositionListItemCard,
  PositionListItemCardSkeleton,
} from "@/entities/position";
import { Card, CardContent } from "@/shared/ui/card";
import { Skeleton } from "@/shared/ui/skeleton";

import { useInfiniteDepartmentPositions } from "../model/use-infinite-department-positions";
import { LoadMore, QueryError } from "./query-controls";

const PAGE_SIZE = 6;

export function DepartmentPositions({ id }: { id: string }) {
  const {
    items,
    totalCount,
    isPending,
    isError,
    refetch,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
  } = useInfiniteDepartmentPositions({
    id,
    pageSize: PAGE_SIZE,
    sortBy: "name",
    sortDirection: "asc",
  });

  return (
    <section aria-label="Должности подразделения">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b pb-4">
        <h3 className="text-sm font-medium">Должности подразделения</h3>
        {isPending ? (
          <Skeleton
            className="h-4 w-24 motion-reduce:animate-none"
            aria-hidden="true"
          />
        ) : totalCount !== undefined ? (
          <p className="text-xs text-muted-foreground">
            Должностей: {totalCount}
          </p>
        ) : null}
      </div>

      {isPending ? (
        <div role="status">
          <p className="sr-only">Загружаем должности…</p>
          <div className="grid gap-5 lg:grid-cols-2" aria-hidden="true">
            {Array.from({ length: PAGE_SIZE }, (_, index) => (
              <PositionListItemCardSkeleton key={index} />
            ))}
          </div>
        </div>
      ) : (
        <>
          {!!items?.length && (
            <ul className="grid gap-5 lg:grid-cols-2">
              {items.map((position) => (
                <li key={position.id} className="min-w-0">
                  <PositionListItemCard position={position} />
                </li>
              ))}
            </ul>
          )}

          {isError ? (
            <QueryError
              message="Не удалось загрузить должности."
              onRetry={() => void refetch()}
            />
          ) : !items?.length ? (
            <Card className="shadow-sm [--card-spacing:--spacing(6)]">
              <CardContent
                role="status"
                className="flex flex-col items-center py-10 text-center"
              >
                <span className="mb-6 flex size-14 items-center justify-center bg-violet-500/10 text-violet-600 dark:text-violet-400">
                  <BriefcaseIcon
                    size={30}
                    weight="duotone"
                    aria-hidden="true"
                  />
                </span>
                <h4 className="text-lg font-semibold">Должностей пока нет</h4>
                <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
                  В этом подразделении ещё нет должностей.
                </p>
              </CardContent>
            </Card>
          ) : null}

          {hasNextPage && !isError && (
            <div className="mt-5 flex justify-center">
              <LoadMore
                isFetching={isFetchingNextPage}
                onLoadMore={() => void fetchNextPage()}
              />
            </div>
          )}
        </>
      )}
    </section>
  );
}
