import { useState } from "react";
import type { LocationListState } from "../model/use-location-list";

import { LocationList } from "./location-list";

import {
  LocationListLoading,
  LocationListError,
  LocationListEmpty,
} from "./location-list-states";
import { LocationListItemDto } from "@/entities/location";
import { UpdateLocationDialog } from "./update-location-dialog";

interface LocationListContentProps {
  state: LocationListState;
  onRetry: () => void;
}

export function LocationListContent({
  state,
  onRetry,
}: LocationListContentProps) {
  const [updatingLocation, setUpdatingLocation] =
    useState<LocationListItemDto | null>(null);

  switch (state.status) {
    case "loading":
      return <LocationListLoading />;

    case "error":
      return <LocationListError message={state.message} onRetry={onRetry} />;

    case "success":
      if (state.locations.length === 0) {
        return <LocationListEmpty />;
      }

      return (
        <>
          <LocationList
            locations={state.locations}
            onCardUpdate={setUpdatingLocation}
            onCardDelete={() => console.log("потом сделаем")}
          />
          {updatingLocation && (
            <UpdateLocationDialog
              location={updatingLocation}
              onClose={() => setUpdatingLocation(null)}
            />
          )}
        </>
      );
  }
}
