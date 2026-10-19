"use client";

import { type LocationListItemDto, locationsApi } from "@/entities/location";
import { ApiRequestError } from "@/shared/api";
import { useQuery } from "@tanstack/react-query";

export type LocationListState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "success"; locations: LocationListItemDto[]; totalCount: number };

export type UseLocationListProps = {
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: string;
};

export function useLocationList(props: UseLocationListProps) {
  let state: LocationListState = {
    status: "loading",
  };

  const { data, isLoading, error, refetch } = useQuery({
    queryFn: () =>
      locationsApi.getLocationsList({
        sortBy: props.sortBy,
        sortDirection: props.sortDirection,
        page: props.page,
        pageSize: props.pageSize,
      }),
    queryKey: [
      "locations",
      {
        page: props.page,
        pageSize: props.pageSize,
      },
    ],
  });

  if (error) {
    let message = "Не удалось загрузить локации";

    if (error instanceof ApiRequestError) {
      if (error.type == "network") {
        message = "Проверьте подключение к интернету";
      }
    }

    state = { status: "error", message: message };
  } else if (isLoading) {
    state = {
      status: "loading",
    };
  } else {
    state = {
      status: "success",
      locations: data?.results ?? [],
      totalCount: data?.totalCount ?? 0,
    };
  }

  return {
    state,
    retry: () => {
      void refetch();
    },
  };
}
