import Link from "next/link";
import { ROUTES } from "@/shared/config/routes";

export default function AppHeader() {
  return (
    <header className="flex items-center justify-center p-5.5 border-b sticky top-0 z-10 bg-muted/90 backdrop-blur-md shadow-md">
      <Link href={ROUTES.HOME} className="font-sans font-extrabold text-3xl">
        notCRM
      </Link>
    </header>
  );
}
