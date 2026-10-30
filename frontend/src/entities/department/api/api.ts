import { apiRequest, type PagedResult } from "@/shared/api";
import {
  DepartmentDto,
  DepartmentBranchNodeDto,
  DepartmentListItemDto,
  DepartmentNodeDto,
  DepartmentDescendantDto,
  TransferredDepartmentDto,
} from "../model/types";
import { infiniteQueryOptions, queryOptions } from "@tanstack/react-query";
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

function normalizeDepartmentListRequest(request: GetDepartmentListRequest) {
  return {
    ...request,
    locationIds:
      request.locationIds === undefined
        ? undefined
        : [...new Set(request.locationIds)].sort(),
    excludeIds:
      request.excludeIds === undefined
        ? undefined
        : [...new Set(request.excludeIds)].sort(),
  };
}

export type GetRootDepartmentsRequest = {
  page?: number;
  pageSize?: number;
};

export type GetImmediateChildrenRequest = {
  page?: number;
  pageSize?: number;
};

export type UpdateDepartmentRequest = {
  id: string;
  name?: string;
  slug?: string;
};

export type CreateDepartmentRequest = {
  name: string;
  slug: string;
  parentId: string | null;
  locationIds: string[];
};

export type DeleteDepartmentRequest = {
  id: string;
};

export type TransferDepartmentRequest = {
  id: string;
  newParentId: string | null;
};

export const departmentsApi = {
  getDepartmentBranch: async (
    id: string,
    options?: Pick<AxiosRequestConfig, "signal" | "timeout">,
  ) =>
    apiRequest<DepartmentBranchNodeDto[]>({
      ...options,
      method: "GET",
      url: `departments/${id}/branch`,
    }),

  transferDepartmentAsync: async ({ id, ...body }: TransferDepartmentRequest) =>
    apiRequest<TransferredDepartmentDto>({
      method: "PUT",
      url: `departments/${id}/parent`,
      data: body,
    }),

  getDepartmentDescendants: async (
    id: string,
    options?: Pick<AxiosRequestConfig, "signal" | "timeout">,
  ) =>
    apiRequest<DepartmentDescendantDto[]>({
      ...options,
      method: "GET",
      url: `departments/${id}/descendants`,
    }),

  createDepartmentAsync: async (request: CreateDepartmentRequest) =>
    apiRequest<DepartmentDto>({
      method: "POST",
      url: "departments",
      data: request,
    }),

  restoreDepartmentAsync: async ({ id }: { id: string }) =>
    apiRequest<void>({
      method: "POST",
      url: `departments/${id}/restore`,
    }),

  updateDepartmentAsync: async ({ id, ...body }: UpdateDepartmentRequest) =>
    apiRequest<string>({
      method: "PATCH",
      url: `departments/${id}`,
      data: body,
    }),

  deleteDepartmentAsync: async ({ id }: DeleteDepartmentRequest) =>
    apiRequest<void>({
      method: "DELETE",
      url: `departments/${id}`,
    }),

  getDepartment: async (
    id: string,
    options?: Pick<AxiosRequestConfig, "signal" | "timeout">,
  ) =>
    apiRequest<DepartmentDto>({
      ...options,
      method: "GET",
      url: `departments/${id}`,
    }),

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
  baseKey: "departments",

  getDepartmentBranchOptions: (id: string) =>
    queryOptions({
      queryKey: [departmentsQueryOptions.baseKey, "branch", id],
      queryFn: ({ signal }) => departmentsApi.getDepartmentBranch(id, { signal }),
    }),

  getDepartmentDescendantsOptions: (id: string) =>
    queryOptions({
      queryKey: [departmentsQueryOptions.baseKey, "descendants", id],
      queryFn: ({ signal }) =>
        departmentsApi.getDepartmentDescendants(id, { signal }),
    }),

  getDepartmentOptions: (id: string) =>
    queryOptions({
      queryKey: [departmentsQueryOptions.baseKey, "detail", id],
      queryFn: ({ signal }) => departmentsApi.getDepartment(id, { signal }),
    }),

  getDepartmentListOptions: (request: GetDepartmentListRequest) => {
    const normalizedRequest = normalizeDepartmentListRequest(request);

    return queryOptions({
      queryKey: [
        departmentsQueryOptions.baseKey,
        "list",
        "paged",
        normalizedRequest,
      ],
      queryFn: () => departmentsApi.getDepartmentList(normalizedRequest),
    });
  },

  getDepartmentInfiniteOptions: (
    request: Omit<GetDepartmentListRequest, "page">,
  ) => {
    const normalizedRequest = normalizeDepartmentListRequest(request);

    return infiniteQueryOptions({
      queryKey: [
        departmentsQueryOptions.baseKey,
        "list",
        "infinite",
        normalizedRequest,
      ],
      queryFn: ({ pageParam, signal }) => {
        return departmentsApi.getDepartmentList(
          {
            ...normalizedRequest,
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

  getRootDepartmentsInfiniteOptions: (
    request: Omit<GetRootDepartmentsRequest, "page">,
  ) => {
    return infiniteQueryOptions({
      queryKey: [departmentsQueryOptions.baseKey, "tree", "roots", request],
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
    request: Omit<GetImmediateChildrenRequest, "page">,
  ) => {
    return infiniteQueryOptions({
      queryKey: [
        departmentsQueryOptions.baseKey,
        "tree",
        "children",
        id,
        request,
      ],
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
