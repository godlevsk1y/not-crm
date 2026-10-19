"use client";

import { useState } from "react";

import { ListPagination } from "@/shared/ui/list-pagination";
import { Skeleton } from "@/shared/ui/skeleton";

import { useDepartmentList } from "../model/use-department-list";
import { DepartmentListContent } from "./department-list-content";

const PAGE_SIZE = 9;

export function DepartmentListSection() {
  const [page, setPage] = useState(1);

  const { state, retry } = useDepartmentList({
    page,
    pageSize: PAGE_SIZE,
    sortBy: "name",
    sortDirection: "asc",
  });

  const isLoading = state.status === "loading";

  return (
    <section aria-label="Подразделения организации" aria-busy={isLoading}>
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

      <DepartmentListContent state={state} onRetry={retry} />

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
