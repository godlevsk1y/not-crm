import { apiRequest, type PagedResult } from "@/shared/api";
import type { LocationDto, LocationListItemDto } from "../model/types";
import { queryOptions } from "@tanstack/react-query";

export type GetLocationsListRequest = {
  search?: string;
  isActive?: boolean;
  minDepartmentCount?: number;
  departmentIds?: string[];
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

export type UpdateLocationRequest = {
  id: string;
  name?: string;
  country?: string;
  region?: string;
  city?: string;
  district?: string;
  street?: string;
  houseNumber?: string;
  postalCode?: string;
};

export type DeleteLocationRequest = {
  id: string;
};

export const locationsApi = {
  getLocationsList: async (request: GetLocationsListRequest) =>
    apiRequest<PagedResult<LocationListItemDto>>({
      method: "GET",
      url: "locations",
      params: {
        ...request,
        departmentIds: request.departmentIds?.join(",") || undefined,
      },
    }),

  createLocationAsync: async (request: CreateLocationRequest) =>
    apiRequest<LocationDto>({
      method: "POST",
      url: "locations",
      data: request,
    }),

  updateLocationAsync: async ({ id, ...body }: UpdateLocationRequest) => {
    return apiRequest<string>({
      method: "PATCH",
      url: `locations/${id}`,
      data: body,
    });
  },

  deleteLocationAsync: async (request: DeleteLocationRequest) =>
    apiRequest<void>({
      method: "DELETE",
      url: `locations/${request.id}`,
    }),
};

export const locationsQueryOptions = {
  baseKey: "locations",

  getLocationOptions: ({
    sortBy,
    sortDirection,
    page,
    pageSize,
    departmentIds,
    isActive = true,
  }: {
    sortBy: string;
    sortDirection: string;
    page: number;
    pageSize: number;
    departmentIds?: string[];
    isActive?: boolean;
  }) => {
    return queryOptions({
      queryFn: () =>
        locationsApi.getLocationsList({
          sortBy: sortBy,
          sortDirection: sortDirection,
          page: page,
          pageSize: pageSize,
          departmentIds,
          isActive,
        }),
      queryKey: [
        locationsQueryOptions.baseKey,
        {
          sortBy: sortBy,
          sortDirection: sortDirection,
          page: page,
          pageSize: pageSize,
          departmentIds,
          isActive,
        },
      ],
    });
  },
};
