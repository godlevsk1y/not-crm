"use client";

import type { DepartmentListItemDto } from "@/entities/department";
import {
  departmentsApi,
  type GetDepartmentListRequest,
} from "@/entities/department/api/api";
import { usePagedList } from "@/shared/lib/hooks/use-paged-list";

export type DepartmentListState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | {
      status: "success";
      departments: DepartmentListItemDto[];
      totalCount: number;
    };

export type UseDepartmentListProps = Pick<
  GetDepartmentListRequest,
  "page" | "pageSize"
> & Partial<Pick<GetDepartmentListRequest, "sortBy" | "sortDirection">>;

export function useDepartmentList({
  page,
  pageSize,
  sortBy = "name",
  sortDirection = "desc",
}: UseDepartmentListProps) {
  const request: GetDepartmentListRequest = {
    page,
    pageSize,
    sortBy,
    sortDirection,
  };

  const { state, retry } = usePagedList({
    queryKey: "departments",
    request,
    queryFn: departmentsApi.getDepartmentList,
    errorMessage: "Не удалось загрузить подразделения",
  });

  return {
    state:
      state.status === "success"
        ? {
            status: state.status,
            departments: state.results,
            totalCount: state.totalCount,
          }
        : state,
    retry,
  };
}
