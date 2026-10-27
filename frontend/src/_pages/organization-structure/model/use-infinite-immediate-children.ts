"use client";

import {
  departmentsQueryOptions,
  GetImmediateChildrenRequest,
} from "@/entities/department";
import { ApiRequestError } from "@/shared/api";
import { useInfiniteQuery } from "@tanstack/react-query";

export type UseInfiniteImmediateChildrenProps = Omit<
  GetImmediateChildrenRequest,
  "page"
> & { id: string };

export function useInfiniteImmediateChildren({
  id: parentId,
  pageSize,
}: UseInfiniteImmediateChildrenProps) {
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
    departmentsQueryOptions.getImmediateChildrenInfiniteOptions(parentId, {
      pageSize,
    }),
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
