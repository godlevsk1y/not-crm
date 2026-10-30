"use client";

import { useEffect } from "react";
import { useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";

import { departmentsQueryOptions } from "@/entities/department";
import { useTreeStore } from "./tree-store";

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function useSelectedDepartment() {
  const searchParams = useSearchParams();
  const rawId = searchParams.get("departmentId");
  const isInvalidId = rawId !== null && !UUID_PATTERN.test(rawId);
  const selectedId = rawId && !isInvalidId ? rawId.toLowerCase() : undefined;
  const branchQuery = useQuery({
    ...departmentsQueryOptions.getDepartmentBranchOptions(selectedId ?? ""),
    enabled: Boolean(selectedId),
    retry: false,
  });
  const branch = branchQuery.data;
  const ancestorIds = branch?.slice(0, -1).map((node) => node.id).join(",");
  const expandNodes = useTreeStore((state) => state.expandNodes);

  useEffect(() => {
    if (ancestorIds) expandNodes(ancestorIds.split(","));
  }, [selectedId, ancestorIds, expandNodes]);

  function selectDepartment(id?: string, replace = false) {
    const url = new URL(window.location.href);
    if (id) url.searchParams.set("departmentId", id);
    else url.searchParams.delete("departmentId");

    if (url.href === window.location.href) return;
    const href = `${url.pathname}${url.search}${url.hash}`;
    if (replace) window.history.replaceState(null, "", href);
    else window.history.pushState(null, "", href);
  }

  return {
    selectedId,
    selectDepartment,
    branch,
    department: branch?.at(-1),
    isInvalidId,
    isPending: Boolean(selectedId) && branchQuery.isPending,
    isError: branchQuery.isError,
    error: branchQuery.error,
    refetch: branchQuery.refetch,
  };
}
