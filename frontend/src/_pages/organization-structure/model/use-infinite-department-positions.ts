"use client";

import {
  GetDepartmentPositionsRequest,
  positionsQueryOptions,
} from "@/entities/position";
import { ApiRequestError } from "@/shared/api";
import { useInfiniteQuery } from "@tanstack/react-query";

export type UseInfiniteDepartmentPositionsProps = Omit<
  GetDepartmentPositionsRequest,
  "page"
> & { id: string };

export function useInfiniteDepartmentPositions({
  id: departmentId,
  ...request
}: UseInfiniteDepartmentPositionsProps) {
  const {
    data,
    isPending,
    isError,
    error,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
    refetch,
  } = useInfiniteQuery(
    positionsQueryOptions.getDepartmentPositionsInfiniteOptions(
      departmentId,
      request,
    ),
  );

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
