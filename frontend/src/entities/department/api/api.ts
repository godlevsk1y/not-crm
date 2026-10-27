import { apiRequest, type PagedResult } from "@/shared/api";
import { DepartmentListItemDto, DepartmentNodeDto } from "../model/types";
import { infiniteQueryOptions } from "@tanstack/react-query";
import type { AxiosRequestConfig } from "axios";

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

export type GetRootDepartmentsRequest = {
  page?: number;
  pageSize?: number;
};

export type GetImmediateChildrenRequest = {
  page?: number;
  pageSize?: number;
};

export const departmentsApi = {
  getDepartmentList: async (
    request: GetDepartmentListRequest,
    options?: Pick<AxiosRequestConfig, "signal" | "timeout">,
  ) =>
    apiRequest<PagedResult<DepartmentListItemDto>>({
      ...options,
      method: "GET",
      url: "departments",
      params: request,
      paramsSerializer: { indexes: null },
    }),

  getRootDepartments: async (request: GetRootDepartmentsRequest) =>
    apiRequest<PagedResult<DepartmentNodeDto>>({
      method: "GET",
      url: "departments/tree",
      params: request,
    }),

  getImmediateChildren: async (
    id: string,
    request: GetImmediateChildrenRequest,
  ) =>
    apiRequest<PagedResult<DepartmentNodeDto>>({
      method: "GET",
      url: `departments/${id}/children`,
      params: request,
    }),
};

export const departmentsQueryOptions = {
  baseKey: ["departments"],

  getDepartmentInfiniteOptions: (request: GetDepartmentListRequest) => {
    return infiniteQueryOptions({
      queryKey: [departmentsQueryOptions.baseKey, request],
      queryFn: ({ pageParam, signal }) => {
        return departmentsApi.getDepartmentList(
          {
            ...request,
            page: pageParam,
          },
          { signal, timeout: 10_000 },
        );
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
        totalCount: data.pages[0].totalCount,
      }),
    });
  },

  getRootDepartmentsInfiniteOptions: (request: GetRootDepartmentsRequest) => {
    return infiniteQueryOptions({
      queryKey: [departmentsQueryOptions.baseKey, request],
      queryFn: ({ pageParam }) => {
        return departmentsApi.getRootDepartments({
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
      select: (data): PagedResult<DepartmentNodeDto> => ({
        results: data.pages.flatMap((page) => page.results),
        page: data.pages[0].page,
        pageSize: data.pages[0].pageSize,
        totalCount: data.pages[0].totalCount,
      }),
    });
  },

  getImmediateChildrenInfiniteOptions: (
    id: string,
    request: GetRootDepartmentsRequest,
  ) => {
    return infiniteQueryOptions({
      queryKey: [departmentsQueryOptions.baseKey, id, request],
      queryFn: ({ pageParam }) => {
        return departmentsApi.getImmediateChildren(id, {
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
      select: (data): PagedResult<DepartmentNodeDto> => ({
        results: data.pages.flatMap((page) => page.results),
        page: data.pages[0].page,
        pageSize: data.pages[0].pageSize,
        totalCount: data.pages[0].totalCount,
      }),
    });
  },
};
