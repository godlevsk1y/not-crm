import { PagedResult, SortDirection } from "@/shared/api/types";
import type { LocationListItemDto } from "../model/types";
import { apiRequest } from "@/shared/api/api-request";

export type GetLocationsListRequest = {
  search?: string;
  minDepartmentCount?: number;
  sortBy: "name" | "createdAt" | "departmentCount";
  sortDirection: SortDirection;
  page: number;
  pageSize: number;
};

export const locationsApi = {
  getLocationsList: async (request: GetLocationsListRequest) =>
    apiRequest<PagedResult<LocationListItemDto>>({
      method: "GET",
      url: "locations",
      params: request,
    }),
};
