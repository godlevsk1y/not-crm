"use client";

import { useQuery } from "@tanstack/react-query";

import { ApiRequestError } from "@/shared/api/api-request-error";
import type { PagedResult } from "@/shared/api/types";

type PagedListState<T> =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "success"; results: T[]; totalCount: number };

type UsePagedListProps<T, TRequest> = {
  queryKey: string;
  request: TRequest;
  queryFn: (request: TRequest) => Promise<PagedResult<T>>;
  errorMessage: string;
};

export function usePagedList<T, TRequest>({
  queryKey,
  request,
  queryFn,
  errorMessage,
}: UsePagedListProps<T, TRequest>) {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: [queryKey, request],
    queryFn: () => queryFn(request),
  });

  function getState(): PagedListState<T> {
    if (error) {
      return {
        status: "error",
        message:
          error instanceof ApiRequestError && error.type === "network"
            ? "Проверьте подключение к интернету"
            : errorMessage,
      };
    }

    if (isLoading) {
      return { status: "loading" };
    }

    return {
      status: "success",
      results: data?.results ?? [],
      totalCount: data?.totalCount ?? 0,
    };
  }

  return {
    state: getState(),
    retry: () => {
      void refetch();
    },
  };
}
