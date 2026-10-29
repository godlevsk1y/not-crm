import { apiRequest, PagedResult } from "@/shared/api";
import { PositionListItemDto } from "../model/types";
import { infiniteQueryOptions } from "@tanstack/react-query";
import type { AxiosRequestConfig } from "axios";

export type GetPositionsListRequest = {
  search?: string;
  isActive?: boolean;
  sortBy: string;
  sortDirection: string;
  page: number;
  pageSize: number;
};

export type GetDepartmentPositionsRequest = {
  sortBy?: "name" | "createdAt";
  sortDirection?: "asc" | "desc";
  page?: number;
  pageSize?: number;
};

export type UpdatePositionRequest = {
  id: string;
  name: string;
};

export type CreatePositionRequest = {
  name: string;
};

export type DeletePositionRequest = {
  id: string;
};

export const positionsApi = {
  createPositionAsync: async (request: CreatePositionRequest) =>
    apiRequest<Pick<PositionListItemDto, "id" | "name">>({
      method: "POST",
      url: "positions",
      data: request,
    }),

  updatePositionAsync: async ({ id, ...body }: UpdatePositionRequest) =>
    apiRequest<string>({
      method: "PATCH",
      url: `positions/${id}`,
      data: body,
    }),

  deletePositionAsync: async ({ id }: DeletePositionRequest) =>
    apiRequest<void>({
      method: "DELETE",
      url: `positions/${id}`,
    }),

  getPositionsList: (
    request: GetPositionsListRequest,
    options?: Pick<AxiosRequestConfig, "signal">,
  ) =>
    apiRequest<PagedResult<PositionListItemDto>>({
      ...options,
      method: "GET",
      url: "positions",
      params: request,
    }),

  getDepartmentPositions: (
    id: string,
    request: GetDepartmentPositionsRequest,
  ) =>
    apiRequest<PagedResult<PositionListItemDto>>({
      method: "GET",
      url: `departments/${id}/positions`,
      params: request,
    }),
};

export const positionsQueryOptions = {
  baseKey: "positions",

  getDepartmentPositionsInfiniteOptions: (
    id: string,
    request: Omit<GetDepartmentPositionsRequest, "page">,
  ) => {
    return infiniteQueryOptions({
      queryKey: [positionsQueryOptions.baseKey, "department", id, request],
      queryFn: ({ pageParam }) => {
        return positionsApi.getDepartmentPositions(id, {
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
      select: (data): PagedResult<PositionListItemDto> => ({
        results: data.pages.flatMap((page) => page.results),
        page: data.pages[0].page,
        pageSize: data.pages[0].pageSize,
        totalCount: data.pages[0].totalCount,
      }),
    });
  },

  getPositionInfiniteOptions: ({
    sortBy,
    sortDirection,
    pageSize,
    isActive = true,
  }: {
    sortBy: string;
    sortDirection: string;
    pageSize: number;
    isActive?: boolean;
  }) => {
    return infiniteQueryOptions({
      queryKey: [
        positionsQueryOptions.baseKey,
        "list",
        "infinite",
        { sortBy, sortDirection, pageSize, isActive },
      ],
      queryFn: ({ pageParam, signal }) => {
        return positionsApi.getPositionsList(
          {
            sortBy: sortBy,
            sortDirection: sortDirection,
            page: pageParam ?? 1,
            pageSize: pageSize,
            isActive,
          },
          { signal },
        );
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
