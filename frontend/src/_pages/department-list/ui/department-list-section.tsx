"use client";

import { useState } from "react";

import { ListPagination } from "@/shared/ui/list-pagination";
import { Skeleton } from "@/shared/ui/skeleton";
import { StatusFilter } from "@/shared/ui/status-filter";

import { DepartmentListContent } from "./department-list-content";
import { useDepartmentList } from "../model/use-department-list";

const PAGE_SIZE = 9;

export function DepartmentListSection() {
  const [page, setPage] = useState(1);
  const [isActive, setIsActive] = useState<boolean | undefined>(true);

  const { state, retry } = useDepartmentList({
    page,
    pageSize: PAGE_SIZE,
    sortBy: "name",
    sortDirection: "asc",
    isActive,
  });

  const isLoading = state.status === "loading";

  return (
    <section aria-label="Подразделения организации" aria-busy={isLoading}>
      <div className="mb-6">
        <StatusFilter
          allowAll
          value={isActive}
          onChange={(value) => {
            if (value === isActive) return;

            setIsActive(value);
            setPage(1);
          }}
        />
      </div>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b pb-4">
        <h2 className="text-sm font-medium">Все подразделения</h2>

        {state.status === "loading" ? (
          <Skeleton
            className="h-4 w-36 motion-reduce:animate-none"
            aria-hidden="true"
          />
        ) : state.status === "success" ? (
          <p className="text-xs text-muted-foreground">
            Подразделений: {state.totalCount}
          </p>
        ) : null}
      </div>

      <DepartmentListContent state={state} onRetry={retry} isActive={isActive} />

      {state.status === "success" && (
        <ListPagination
          page={page}
          totalCount={state.totalCount}
          pageSize={PAGE_SIZE}
          onPageChange={(newPage) => setPage(newPage)}
        />
      )}
    </section>
  );
}
