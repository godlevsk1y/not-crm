"use client";

import { positionsQueryOptions } from "@/entities/position";
import { ApiRequestError } from "@/shared/api";
import { useInfiniteQuery } from "@tanstack/react-query";

export function useInfinitePositionList({
  sortBy,
  sortDirection,
  pageSize,
}: {
  sortBy: string;
  sortDirection: string;
  pageSize: number;
}) {
  const {
    data,
    isPending,
    error,
    isError,
    refetch,
    isFetchingNextPage,
    fetchNextPage,
    hasNextPage,
  } = useInfiniteQuery({
    ...positionsQueryOptions.getPositionInfiniteOptions({
      sortBy: sortBy,
      sortDirection: sortDirection,
      pageSize: pageSize,
    }),
  });

  return {
    items: data?.results,
    totalCount: data?.totalCount,
    isPending,
    error: error instanceof ApiRequestError ? error : undefined,
    isError: isError,
    refetch,
    isFetchingNextPage,
    fetchNextPage,
    hasNextPage,
  };
}
