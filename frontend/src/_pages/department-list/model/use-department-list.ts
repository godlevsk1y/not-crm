"use client";

import type { DepartmentListItemDto } from "@/entities/department";
import { departmentsApi } from "@/entities/department/api/api";
import { ApiRequestError } from "@/shared/api";
import { useQuery } from "@tanstack/react-query";

export type DepartmentListState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | {
      status: "success";
      departments: DepartmentListItemDto[];
      totalCount: number;
    };

export type UseDepartmentListProps = {
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: string;
};

export function useDepartmentList(props: UseDepartmentListProps) {
  let state: DepartmentListState = {
    status: "loading",
  };

  const { data, isLoading, error, refetch } = useQuery({
    queryFn: () =>
      departmentsApi.getDepartmentList({
        sortBy: props.sortBy,
        sortDirection: props.sortDirection,
        page: props.page,
        pageSize: props.pageSize,
      }),
    queryKey: [
      "departments",
      {
        page: props.page,
        pageSize: props.pageSize,
      },
    ],
  });

  if (error) {
    let message = "Не удалось загрузить подразделения";

    if (error instanceof ApiRequestError) {
      if (error.type === "network") {
        message = "Проверьте подключение к интернету";
      }
    }

    state = { status: "error", message: message };
  } else if (isLoading) {
    state = { status: "loading" };
  } else {
    state = {
      status: "success",
      departments: data?.results ?? [],
      totalCount: data?.totalCount ?? 0,
    };
  }

  return {
    state,
    retry: () => {
      void refetch();
    },
  };
}
