import { PagedResult } from "@/shared/api/types";
import type { LocationDto, LocationListItemDto } from "../model/types";
import { apiRequest } from "@/shared/api/api-request";

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
