import PositionListHeader from "./position-list-header";
import { PositionListSection } from "./position-list-section";

export function PositionListPage() {
  return (
    <main className="flex min-h-screen flex-col items-center bg-muted/30 px-6 py-10 sm:py-15">
      <div className="w-full max-w-5xl">
        <PositionListHeader />
        <PositionListSection />
      </div>
    </main>
  );
}
