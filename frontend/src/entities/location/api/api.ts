import { apiRequest, type PagedResult } from "@/shared/api";
import type { LocationDto, LocationListItemDto } from "../model/types";
import { queryOptions } from "@tanstack/react-query";

export type GetLocationsListRequest = {
  search?: string;
  minDepartmentCount?: number;
  sortBy: string;
  sortDirection: string;
  page: number;
  pageSize: number;
};

export type CreateLocationRequest = {
  name: string;
  country: string;
  region?: string;
  city: string;
  district?: string;
  street: string;
  houseNumber: string;
  postalCode?: string;
};

export const locationsApi = {
  getLocationsList: async (request: GetLocationsListRequest) =>
    apiRequest<PagedResult<LocationListItemDto>>({
      method: "GET",
      url: "locations",
      params: request,
    }),

  createLocationAsync: async (request: CreateLocationRequest) =>
    apiRequest<LocationDto>({
      method: "POST",
      url: "locations",
      data: request,
    }),
};

export const locationsQueryOptions = {
  baseKey: "locations",

  getLocationOptions: ({
    sortBy,
    sortDirection,
    page,
    pageSize,
  }: {
    sortBy: string;
    sortDirection: string;
    page: number;
    pageSize: number;
  }) => {
    return queryOptions({
      queryFn: () =>
        locationsApi.getLocationsList({
          sortBy: sortBy,
          sortDirection: sortDirection,
          page: page,
          pageSize: pageSize,
        }),
      queryKey: [
        locationsQueryOptions.baseKey,
        {
          sortBy: sortBy,
          sortDirection: sortDirection,
          page: page,
          pageSize: pageSize,
        },
      ],
    });
  },
};
