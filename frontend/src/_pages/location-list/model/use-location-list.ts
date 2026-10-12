"use client";

import { type LocationListItemDto, locationsApi } from "@/entities/location";
import { ApiRequestError } from "@/shared/api/api-request-error";
import { useEffect, useState } from "react";

export type LocationListState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "success"; locations: LocationListItemDto[] };

export function useLocationsList() {
  const [state, setState] = useState<LocationListState>({
    status: "loading",
  });

  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let ignore = false;

    async function loadLocations() {
      try {
        const result = await locationsApi.getLocationsList({
          sortBy: "name",
          sortDirection: "asc",
          page: 1,
          pageSize: 20,
        });

        if (ignore) return;

        setState({ status: "success", locations: result.results });
      } catch (error) {
        if (ignore) return;

        let message = "Не удалось загрузить локации";

        if (error instanceof ApiRequestError) {
          if (error.type == "network") {
            message = "Проверьте подключение к интернету";
          }
        }

        setState({ status: "error", message: message });
      }
    }

    void loadLocations();

    return () => {
      ignore = true;
    };
  }, [attempt]);

  function retry() {
    setState({ status: "loading" });
    setAttempt((value) => value + 1);
  }

  return { state, retry };
}
