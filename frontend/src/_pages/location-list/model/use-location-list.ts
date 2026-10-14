"use client";

import type { LocationListItemDto } from "@/entities/location";
import {
  locationsApi,
  type GetLocationsListRequest,
} from "@/entities/location/api/api";
import { usePagedList } from "@/shared/lib/hooks/use-paged-list";

export type LocationListState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "success"; locations: LocationListItemDto[]; totalCount: number };

export type UseLocationListProps = Pick<
  GetLocationsListRequest,
  "page" | "pageSize"
> & Partial<Pick<GetLocationsListRequest, "sortBy" | "sortDirection">>;

export function useLocationList({
  page,
  pageSize,
  sortBy = "name",
  sortDirection = "asc",
}: UseLocationListProps) {
  const request: GetLocationsListRequest = {
    page,
    pageSize,
    sortBy,
    sortDirection,
  };

  const { state, retry } = usePagedList({
    queryKey: "locations",
    request,
    queryFn: locationsApi.getLocationsList,
    errorMessage: "Не удалось загрузить локации",
  });

  return {
    state:
      state.status === "success"
        ? {
            status: state.status,
            locations: state.results,
            totalCount: state.totalCount,
          }
        : state,
    retry,
  };
}
