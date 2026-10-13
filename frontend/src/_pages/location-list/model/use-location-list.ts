"use client";

import { type LocationListItemDto, locationsApi } from "@/entities/location";
import { ApiRequestError } from "@/shared/api/api-request-error";
import { useQuery } from "@tanstack/react-query";

export type LocationListState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "success"; locations: LocationListItemDto[] };

export type UseLocationListProps = {
  page: number;
  pageSize: number;
};

export function useLocationList(props: UseLocationListProps) {
  let state: LocationListState = {
    status: "loading",
  };

  const {
    data: locations,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryFn: () =>
      locationsApi.getLocationsList({
        sortBy: "name",
        sortDirection: "asc",
        page: props.page,
        pageSize: props.pageSize,
      }),
    queryKey: ["locations"],
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
      locations: locations?.results ?? [],
    };
  }

  return {
    state,
    retry: () => {
      void refetch();
    },
  };
}
