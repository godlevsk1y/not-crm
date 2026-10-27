import { apiRequest, type PagedResult } from "@/shared/api";
import { DepartmentListItemDto } from "../model/types";
import { infiniteQueryOptions } from "@tanstack/react-query";

export type GetDepartmentListRequest = {
  search?: string;
  sortBy?: "name" | "createdAt";
  sortDirection?: "asc" | "desc";
  isActive?: boolean;
  locationIds?: string[];
  excludeIds?: string[];
  page?: number;
  pageSize?: number;
};

export const departmentsApi = {
  getDepartmentList: async (request: GetDepartmentListRequest) =>
    apiRequest<PagedResult<DepartmentListItemDto>>({
      method: "GET",
      url: "departments",
      params: request,
      paramsSerializer: { indexes: null },
    }),
};

export const departmentsQueryOptions = {
  baseKey: ["departments"],

  getDepartmentInfiniteOptions: (request: GetDepartmentListRequest) => {
    return infiniteQueryOptions({
      queryKey: [departmentsQueryOptions.baseKey],
      queryFn: ({ pageParam }) => {
        return departmentsApi.getDepartmentList({
          ...request,
          page: pageParam,
        });
      },
      initialPageParam: 1,
      getNextPageParam: (response) => {
        const totalPages = Math.ceil(response.totalCount / response.pageSize);

        if (response.page >= totalPages) return undefined;

        return response.page + 1;
      },
      select: (data): PagedResult<DepartmentListItemDto> => ({
        results: data.pages.flatMap((page) => page.results),
        page: data.pages[0].page,
        pageSize: data.pages[0].pageSize,
        totalCount: data.pages[0].pageSize,
      }),
    });
  },
};
