import DepartmentListHeader from "./department-list-header";
import { DepartmentListSection } from "./department-list-section";

export function DepartmentListPage() {
  return (
    <main className="flex min-h-screen flex-col items-center bg-muted/30 px-6 py-10 sm:py-15">
      <div className="w-full max-w-5xl">
        <DepartmentListHeader />
        <DepartmentListSection />
      </div>
    </main>
  );
}
