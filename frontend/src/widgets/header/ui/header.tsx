import Link from "next/link";
import { SidebarTrigger } from "@/shared/ui/sidebar";
import { ROUTES } from "@/shared/config/routes";
import { GlobalSearch } from "@/features/global-search";

export function Header() {
  return (
    <header className="flex items-center gap-4 p-5.5 border-b sticky top-0 z-10 bg-muted/90 backdrop-blur-md shadow-md">
      <SidebarTrigger
        aria-label="Открыть или закрыть меню сервисов"
        title="Меню сервисов"
      />
      <Link href={ROUTES.HOME} className="font-sans font-extrabold text-3xl">
        notCRM
      </Link>
      <GlobalSearch />
    </header>
  );
}
