"use client";

import { Skeleton } from "@/shared/ui/skeleton";

import { useLocationsList } from "../model/use-location-list";
import { LocationListContent } from "./location-list-content";

export function LocationListSection() {
  const { state, retry } = useLocationsList();

  const isLoading = state.status === "loading";

  const cityCount =
    state.status === "success"
      ? new Set(state.locations.map((location) => location.address.city)).size
      : 0;

  return (
    <section aria-label="Локации организации" aria-busy={isLoading}>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b pb-4">
        <h2 className="text-sm font-medium">Все локации</h2>

        {state.status === "loading" ? (
          <Skeleton
            className="h-4 w-44 motion-reduce:animate-none"
            aria-hidden="true"
          />
        ) : state.status === "success" ? (
          <p className="flex items-center gap-3 text-xs text-muted-foreground">
            <span>Локаций: {state.locations.length}</span>

            <span aria-hidden="true" className="size-1 bg-border" />

            <span>Городов: {cityCount}</span>
          </p>
        ) : null}
      </div>

      <LocationListContent state={state} onRetry={retry} />
    </section>
  );
}
