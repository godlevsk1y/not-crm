import { apiRequest, PagedResult } from "@/shared/api";
import { PositionListItemDto } from "../model/types";
import { infiniteQueryOptions } from "@tanstack/react-query";

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

export const positionsQueryOptions = {
  baseKey: "positions",

  getPositionInfiniteOptions: ({
    sortBy,
    sortDirection,
    pageSize,
  }: {
    sortBy: string;
    sortDirection: string;
    pageSize: number;
  }) => {
    return infiniteQueryOptions({
      queryKey: [positionsQueryOptions.baseKey],
      queryFn: ({ pageParam }) => {
        return positionsApi.getPositionsList({
          sortBy: sortBy,
          sortDirection: sortDirection,
          page: pageParam ?? 1,
          pageSize: pageSize,
        });
      },
      initialPageParam: 1,
      getNextPageParam: (response) => {
        const totalPages = Math.ceil(response.totalCount / response.pageSize);

        if (response.page >= totalPages) return undefined;

        return response.page + 1;
      },
      select: (data): PagedResult<PositionListItemDto> => ({
        results: data.pages.flatMap((page) => page.results),
        page: data.pages[0].page,
        pageSize: data.pages[0].pageSize,
        totalCount: data.pages[0].totalCount,
      }),
    });
  },
};
