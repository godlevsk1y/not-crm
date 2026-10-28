"use client";

import {
  departmentsQueryOptions,
  GetRootDepartmentsRequest,
} from "@/entities/department";
import { ApiRequestError } from "@/shared/api";
import { useInfiniteQuery } from "@tanstack/react-query";

export type UseInfiniteRootDepartmentsProps = Omit<
  GetRootDepartmentsRequest,
  "page"
>;

export function useInfiniteRootDepartments({
  pageSize,
}: UseInfiniteRootDepartmentsProps) {
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
    departmentsQueryOptions.getRootDepartmentsInfiniteOptions({ pageSize }),
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
