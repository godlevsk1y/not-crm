import { apiRequest, PagedResult } from "@/shared/api";
import { PositionListItemDto } from "../model/types";

export type GetPositionsListRequest = {
  search?: string;
  sortBy: string;
  sortDirection: string;
  page: number;
  pageSize: number;
};

export const positionsApi = {
  getPositionsList: (request: GetPositionsListRequest) =>
    apiRequest<PagedResult<PositionListItemDto>>({
      method: "GET",
      url: "positions",
      params: request,
    }),
};

// TODO: add locationQueryOptions
