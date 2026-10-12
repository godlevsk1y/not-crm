import type { LocationListState } from "../model/use-location-list";

import { LocationList } from "./location-list";

import {
  LocationListLoading,
  LocationListError,
  LocationListEmpty,
} from "./location-list-states";

interface LocationListContentProps {
  state: LocationListState;
  onRetry: () => void;
}

export function LocationListContent({
  state,
  onRetry,
}: LocationListContentProps) {
  switch (state.status) {
    case "loading":
      return <LocationListLoading />;

    case "error":
      return <LocationListError message={state.message} onRetry={onRetry} />;

    case "success":
      if (state.locations.length === 0) {
        return <LocationListEmpty />;
      }

      return <LocationList locations={state.locations} />;
  }
}
