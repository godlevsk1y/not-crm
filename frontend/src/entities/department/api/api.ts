import { apiRequest, type PagedResult } from "@/shared/api";
import { DepartmentListItemDto } from "../model/types";

export type GetDepartmentListRequest = {
  search?: string;
  sortBy: string;
  sortDirection: string;
  page: number;
  pageSize: number;
};

export const departmentsApi = {
  getDepartmentList: async (request: GetDepartmentListRequest) =>
    apiRequest<PagedResult<DepartmentListItemDto>>({
      method: "GET",
      url: "departments",
      params: request,
    }),
};
