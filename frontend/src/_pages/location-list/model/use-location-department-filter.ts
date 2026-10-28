"use client";

import { departmentsApi, departmentsQueryOptions } from "@/entities/department";
import type { DepartmentSelectOption } from "@/features/department-select";
import { ApiRequestError } from "@/shared/api";
import { useQueries, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "next/navigation";

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function getDepartmentOptionKey(id: string) {
  return [departmentsQueryOptions.baseKey, "select-option", id.toLowerCase()];
}

export function useLocationDepartmentFilter() {
  const queryClient = useQueryClient();
  const searchParams = useSearchParams();
  const ids = [
    ...new Set(
      (searchParams.get("departmentIds") ?? "")
        .split(",")
        .map((id) => id.trim().toLowerCase())
        .filter((id) => UUID_PATTERN.test(id)),
    ),
  ];

  const queries = useQueries({
    queries: ids.map((id) => ({
      queryKey: getDepartmentOptionKey(id),
      queryFn: async ({ signal }: { signal: AbortSignal }) => {
        try {
          const department = await departmentsApi.getDepartment(id, {
            signal,
            timeout: 10_000,
          });
          return { id: department.id, name: department.name };
        } catch (error) {
          if (
            error instanceof ApiRequestError &&
            (error.status === 404 || error.type === "not_found")
          ) {
            return null;
          }
          throw error;
        }
      },
      retry: false,
    })),
  });

  const selected: DepartmentSelectOption[] = queries.flatMap((query) =>
    query.data ? [query.data] : [],
  );

  function updateUrlFilters(departments: DepartmentSelectOption[]) {
    for (const department of departments) {
      queryClient.setQueryData<DepartmentSelectOption>(
        getDepartmentOptionKey(department.id),
        { id: department.id, name: department.name },
      );
    }

    const url = new URL(window.location.href);
    url.searchParams.delete("departmentIds");
    const params = url.searchParams.toString();
    const filter =
      departments.length > 0
        ? `departmentIds=${departments.map((department) => department.id).join(",")}`
        : "";
    const query = [params, filter].filter(Boolean).join("&");
    window.history.pushState(
      null,
      "",
      `${url.pathname}${query ? `?${query}` : ""}${url.hash}`,
    );
  }

  return {
    selected,
    updateUrlFilters,
    isPending: queries.some((query) => query.isPending),
    isError: queries.some((query) => query.isError),
    retry: () =>
      queries.forEach((query) => {
        void query.refetch();
      }),
  };
}
