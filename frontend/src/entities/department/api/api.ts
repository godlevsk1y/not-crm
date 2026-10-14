import { apiRequest } from "@/shared/api/api-request";
import { PagedResult, SortDirection } from "@/shared/api/types";
import { DepartmentListItemDto } from "../model/types";

export type GetDepartmentListRequest = {
  search?: string;
  sortBy: "name" | "createdAt";
  sortDirection: SortDirection;
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
