"use client";

import { RefCallback, useCallback, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { positionsQueryOptions } from "@/entities/position";
import { StatusFilter } from "@/shared/ui/status-filter";
import { Skeleton } from "@/shared/ui/skeleton";
import { useInfinitePositionList } from "../model/use-infinite-position-list";
import { PositionListContent } from "./position-list-content";

const PAGE_SIZE = 9;
const LIST_REQUEST = {
  sortBy: "createdAt",
  sortDirection: "desc",
  pageSize: PAGE_SIZE,
};

export function PositionListSection() {
  const [isActive, setIsActive] = useState(true);
  const queryClient = useQueryClient();
  const {
    items: positions,
    isPending,
    isError,
    totalCount,
    refetch,
    isFetchingNextPage,
    fetchNextPage,
    hasNextPage,
  } = useInfinitePositionList({
    ...LIST_REQUEST,
    isActive,
  });

  function changeStatus(value: boolean) {
    if (value === isActive) return;

    // Returning to a previously viewed status must also start from page one.
    queryClient.removeQueries({
      queryKey: positionsQueryOptions.getPositionInfiniteOptions({
        ...LIST_REQUEST,
        isActive: value,
      }).queryKey,
      exact: true,
    });
    setIsActive(value);
  }

  const cursorRef: RefCallback<HTMLDivElement> = useCallback(
    (el) => {
      const observer = new IntersectionObserver(
        (entries) => {
          if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
            fetchNextPage();
          }
        },
        { threshold: 0.8 },
      );

      if (el) {
        observer.observe(el);

        return () => observer.disconnect();
      }
    },
    [fetchNextPage, hasNextPage, isFetchingNextPage],
  );

  return (
    <section aria-label="Должности организации" aria-busy={isPending}>
      <div className="mb-6">
        <StatusFilter value={isActive} onChange={changeStatus} />
      </div>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b pb-4">
        <h2 className="text-sm font-medium">Все должности</h2>
        {isPending ? (
          <Skeleton
            className="h-4 w-36 motion-reduce:animate-none"
            aria-hidden="true"
          />
        ) : !isError ? (
          <p className="text-xs text-muted-foreground">
            Должностей: {totalCount}
          </p>
        ) : null}
      </div>

      <PositionListContent
        positions={positions}
        isError={isError}
        isPending={isPending}
        isActive={isActive}
        pageSize={PAGE_SIZE}
        onRetry={refetch}
        isFetchingNextPage={isFetchingNextPage}
        cursorRef={cursorRef}
      />
    </section>
  );
}
