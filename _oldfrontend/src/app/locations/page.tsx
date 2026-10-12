"use client";

import {
  ArrowClockwiseIcon,
  MapPinIcon,
  WarningCircleIcon,
} from "@phosphor-icons/react/ssr";
import LocationCard from "./_components/location-card";
import LocationCardSkeleton from "./_components/location-card-skeleton";
import { useEffect, useState } from "react";
import type { LocationListItemDto } from "@/entities/locations/types";
import { locationsApi } from "@/entities/locations/api/api";
import { Button } from "@/shared/ui/button";
import { Card, CardContent } from "@/shared/ui/card";
import { Skeleton } from "@/shared/ui/skeleton";

export default function LocationsPage() {
  const [locations, setLocations] = useState<LocationListItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
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

        if (typeof result === "string") {
          setError(result);
          return;
        }

        setLocations(result.results);
      } catch {
        if (!ignore) {
          setError("Не удалось загрузить локации");
        }
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    }

    void loadLocations();

    return () => {
      ignore = true;
    };
  }, [attempt]);

  function retry() {
    setLoading(true);
    setError(null);
    setAttempt((value) => value + 1);
  }

  const cityCount = new Set(locations.map((location) => location.address.city))
    .size;

  return (
    <main className="min-h-screen bg-muted/30 px-6 py-12 sm:py-24">
      <div className="mx-auto w-full max-w-5xl">
        <header className="mb-10">
          <p className="mb-3 text-xs tracking-[0.2em] text-muted-foreground uppercase">
            Directory Service
          </p>
          <div className="flex items-center gap-4">
            <span className="flex size-12 shrink-0 items-center justify-center bg-sky-500/10 text-sky-600 dark:text-sky-400">
              <MapPinIcon size={26} weight="duotone" aria-hidden="true" />
            </span>
            <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">
              Локации
            </h1>
          </div>
          <p className="mt-4 max-w-xl text-sm leading-relaxed text-muted-foreground">
            Места, где работает наша команда. Адреса офисов и рабочих
            пространств, связанных с подразделениями организации.
          </p>
        </header>

        <section aria-label="Локации организации" aria-busy={loading}>
          <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b pb-4">
            <h2 className="text-sm font-medium">Все локации</h2>
            {loading ? (
              <Skeleton
                className="h-4 w-44 motion-reduce:animate-none"
                aria-hidden="true"
              />
            ) : error === null ? (
              <p className="flex items-center gap-3 text-xs text-muted-foreground">
                <span>Локаций: {locations.length}</span>
                <span aria-hidden="true" className="size-1 bg-border" />
                <span>Городов: {cityCount}</span>
              </p>
            ) : null}
          </div>
          {loading ? (
            <div role="status">
              <p className="sr-only">Загружаем локации…</p>
              <div
                className="grid gap-5 md:grid-cols-2 xl:grid-cols-3"
                aria-hidden="true"
              >
                {Array.from({ length: 6 }, (_, index) => (
                  <LocationCardSkeleton key={index} />
                ))}
              </div>
            </div>
          ) : error !== null ? (
            <Card className="shadow-sm [--card-spacing:--spacing(6)]">
              <CardContent className="flex flex-col items-center py-10 text-center sm:py-16">
                <div
                  role="alert"
                  className="flex max-w-sm flex-col items-center"
                >
                  <span className="mb-6 flex size-14 items-center justify-center bg-destructive/10 text-destructive">
                    <WarningCircleIcon
                      size={30}
                      weight="duotone"
                      aria-hidden="true"
                    />
                  </span>
                  <h3 className="text-lg font-semibold">
                    Не удалось загрузить локации
                  </h3>
                  <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
                    Возникла ошибка при получении данных. Попробуйте загрузить
                    список ещё раз.
                  </p>
                </div>
                <Button
                  variant="outline"
                  size="lg"
                  className="mt-6"
                  onClick={retry}
                >
                  <ArrowClockwiseIcon aria-hidden="true" />
                  Попробовать снова
                </Button>
              </CardContent>
            </Card>
          ) : locations.length === 0 ? (
            <Card className="shadow-sm [--card-spacing:--spacing(6)]">
              <CardContent
                role="status"
                className="flex flex-col items-center py-10 text-center sm:py-16"
              >
                <span className="mb-6 flex size-14 items-center justify-center bg-sky-500/10 text-sky-600 dark:text-sky-400">
                  <MapPinIcon size={30} weight="duotone" aria-hidden="true" />
                </span>
                <h3 className="text-lg font-semibold">Локаций пока нет</h3>
                <p className="mt-3 max-w-sm text-sm leading-relaxed text-muted-foreground">
                  Здесь появятся адреса офисов и рабочих пространств, когда в
                  справочник добавят первую локацию.
                </p>
              </CardContent>
            </Card>
          ) : (
            <ul className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
              {locations.map((location) => (
                <li key={location.id} className="min-w-0">
                  <LocationCard location={location} />
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </main>
  );
}
