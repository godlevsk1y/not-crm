import { useState } from "react";
import type { LocationListState } from "../model/use-location-list";

import { LocationList } from "./location-list";

import {
  LocationListLoading,
  LocationListError,
  LocationListEmpty,
} from "./location-list-states";
import { LocationListItemDto } from "@/entities/location";
import {
  UpdateLocationDialog,
  DeleteLocationDialog,
} from "@/features/location-actions";

interface LocationListContentProps {
  state: LocationListState;
  onRetry: () => void;
  isFiltered?: boolean;
  isActive: boolean;
}

export function LocationListContent({
  state,
  onRetry,
  isFiltered = false,
  isActive,
}: LocationListContentProps) {
  const [updatingLocation, setUpdatingLocation] =
    useState<LocationListItemDto | null>(null);
  const [deletingLocation, setDeletingLocation] =
    useState<LocationListItemDto | null>(null);

  switch (state.status) {
    case "loading":
      return <LocationListLoading />;

    case "error":
      return <LocationListError message={state.message} onRetry={onRetry} />;

    case "success":
      if (state.locations.length === 0) {
        return <LocationListEmpty isFiltered={isFiltered} isActive={isActive} />;
      }

      return (
        <>
          <LocationList
            locations={state.locations}
            onCardUpdate={setUpdatingLocation}
            onCardDelete={setDeletingLocation}
          />
          {updatingLocation && (
            <UpdateLocationDialog
              location={updatingLocation}
              onClose={() => setUpdatingLocation(null)}
            />
          )}
          {deletingLocation && (
            <DeleteLocationDialog
              location={deletingLocation}
              onClose={() => setDeletingLocation(null)}
            />
          )}
        </>
      );
  }
}
