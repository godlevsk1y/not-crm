import LocationListHeader from "./location-list-header";
import { LocationListSection } from "./location-list-section";
import { Suspense } from "react";
import { LocationListLoading } from "./location-list-states";

export function LocationListPage() {
  return (
    <main className="flex min-h-screen flex-col items-center bg-muted/30 px-6 py-10 sm:py-15">
      <div className="w-full max-w-5xl">
        <LocationListHeader />
        <Suspense fallback={<LocationListLoading />}>
          <LocationListSection />
        </Suspense>
      </div>
    </main>
  );
}
