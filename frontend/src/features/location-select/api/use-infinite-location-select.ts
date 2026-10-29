"use client";

import {
  locationsQueryOptions,
  GetLocationsListRequest,
} from "@/entities/location";
import { ApiRequestError } from "@/shared/api";
import { useInfiniteQuery } from "@tanstack/react-query";

export type UseInfiniteLocationSelect = Omit<
  GetLocationsListRequest,
  "page"
>;

export function useInfiniteLocationSelect(
  props: UseInfiniteLocationSelect,
  enabled: boolean,
) {
  const {
    data,
    isPending,
    error,
    isError,
    refetch,
    isFetchNextPageError,
    isFetchingNextPage,
    fetchNextPage,
    hasNextPage,
  } = useInfiniteQuery({
    ...locationsQueryOptions.getLocationInfiniteOptions(props),
    enabled,
    networkMode: "always",
    retry: false,
    refetchOnReconnect: true,
  });

  return {
    items: data?.results,
    totalCount: data?.totalCount,
    isPending,
    error: error instanceof ApiRequestError ? error : undefined,
    isError: isError,
    refetch,
    isFetchNextPageError,
    isFetchingNextPage,
    fetchNextPage,
    hasNextPage,
  };
}
