"use client";

import { Skeleton } from "@/shared/ui/skeleton";

import { useLocationList } from "../model/use-location-list";
import { LocationListContent } from "./location-list-content";
import { ListPagination } from "@/shared/ui/list-pagination";
import { useState } from "react";
import { DepartmentSelect } from "@/features/department-select";
import { useLocationDepartmentFilter } from "../model/use-location-department-filter";
import { LocationListError, LocationListLoading } from "./location-list-states";

const PAGE_SIZE = 9;

export function LocationListSection() {
  const filter = useLocationDepartmentFilter();
  const departmentIds = filter.selected.map((department) => department.id);

  return (
    <section aria-label="Локации организации">
      <div className="mb-6 max-w-lg">
        <DepartmentSelect
          mode="multiple"
          label="Фильтр по подразделениям"
          placeholder="Все подразделения"
          value={filter.selected}
          onChange={filter.onChange}
          disabled={filter.isPending}
        />
      </div>

      {filter.isError ? (
        <LocationListError
          message="Не удалось загрузить выбранные подразделения"
          onRetry={filter.retry}
        />
      ) : filter.isPending ? (
        <LocationListLoading />
      ) : (
        <LocationListResults
          key={departmentIds.join(",")}
          departmentIds={departmentIds}
        />
      )}
    </section>
  );
}

function LocationListResults({ departmentIds }: { departmentIds: string[] }) {
  const [page, setPage] = useState(1);

  const { state, retry } = useLocationList({
    page: page,
    pageSize: PAGE_SIZE,
    sortBy: "name",
    sortDirection: "asc",
    departmentIds,
  });

  const isLoading = state.status === "loading";

  const cityCount =
    state.status === "success"
      ? new Set(state.locations.map((location) => location.address.city)).size
      : 0;

  return (
    <div aria-busy={isLoading}>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b pb-4">
        <h2 className="text-sm font-medium">
          {departmentIds.length > 0
            ? "Локации выбранных подразделений"
            : "Все локации"}
        </h2>

        {state.status === "loading" ? (
          <Skeleton
            className="h-4 w-44 motion-reduce:animate-none"
            aria-hidden="true"
          />
        ) : state.status === "success" ? (
          <p className="flex items-center gap-3 text-xs text-muted-foreground">
            <span>Локаций: {state.totalCount}</span>

            <span aria-hidden="true" className="size-1 bg-border" />

            <span>Городов: {cityCount}</span>
          </p>
        ) : null}
      </div>

      <LocationListContent
        state={state}
        onRetry={retry}
        isFiltered={departmentIds.length > 0}
      />

      {state.status === "success" && (
        <ListPagination
          page={page}
          totalCount={state.totalCount}
          pageSize={PAGE_SIZE}
          onPageChange={(newPage) => setPage(newPage)}
        />
      )}
    </div>
  );
}
