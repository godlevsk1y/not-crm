"use client";

import { RefCallback, useCallback } from "react";
import { useInfinitePositionList } from "../model/use-infinite-position-list";
import { PositionListContent } from "./position-list-content";

const PAGE_SIZE = 9;

export function PositionListSection() {
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
    sortBy: "createdAt",
    sortDirection: "desc",
    pageSize: PAGE_SIZE,
  });

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
    <section aria-label="Должности организации">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b pb-4">
        <h2 className="text-sm font-medium">Все должности</h2>
        <p className="text-xs text-muted-foreground">
          Должностей: {totalCount}
        </p>
      </div>

      <PositionListContent
        positions={positions}
        isError={isError}
        isPending={isPending}
        pageSize={PAGE_SIZE}
        onRetry={refetch}
        isFetchingNextPage={isFetchingNextPage}
        cursorRef={cursorRef}
      />
    </section>
  );
}
