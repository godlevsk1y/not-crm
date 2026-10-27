"use client";

import {
  departmentsQueryOptions,
  GetDepartmentListRequest,
} from "@/entities/department";
import { ApiRequestError } from "@/shared/api";
import { useInfiniteQuery } from "@tanstack/react-query";

export type UseInfiniteDepartmentSelect = Omit<
  GetDepartmentListRequest,
  "page"
>;

export function useInfiniteDepartmentSelect(
  props: UseInfiniteDepartmentSelect,
) {
  const {
    data,
    isPending,
    error,
    isError,
    refetch,
    isFetchingNextPage,
    fetchNextPage,
    hasNextPage,
  } = useInfiniteQuery(
    departmentsQueryOptions.getDepartmentInfiniteOptions(props),
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
