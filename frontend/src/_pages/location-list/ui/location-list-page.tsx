import LocationListHeader from "./location-list-header";
import { LocationListSection } from "./location-list-section";

export function LocationListPage() {
  return (
    <main className="flex min-h-screen flex-col items-center bg-muted/30 px-6 py-10 sm:py-15">
      <div className="w-full max-w-5xl">
        <LocationListHeader />
        <LocationListSection sortBy="name" sortDirection="asc" />
      </div>
    </main>
  );
}
