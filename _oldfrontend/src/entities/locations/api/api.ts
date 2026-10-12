import { ApiEnvelope, PagedResult } from "@/shared/api/api-responses";
import { apiClient } from "@/shared/api/axios-instance";
import { LocationListItemDto } from "../types";

export type GetLocationsListRequest = {
  search?: string;
  minDepartmentCount?: number;
  sortBy: string;
  sortDirection: string;
  page: number;
  pageSize: number;
};

export const locationsApi = {
  getLocationsList: async (
    request: GetLocationsListRequest,
  ): Promise<PagedResult<LocationListItemDto> | string> => {
    const { data } = await apiClient.get<
      ApiEnvelope<PagedResult<LocationListItemDto>>
    >("/locations", { params: request });

    if (data.result === null) {
      return "Не удалось загрузить данные";
    }

    return data.result!;
  },
};
