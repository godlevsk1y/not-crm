import { TreeStructureIcon } from "@phosphor-icons/react/ssr";
import { Suspense } from "react";

import { OrganizationStructureSection } from "./organization-structure-section";

export function OrganizationStructurePage() {
  return (
    <main className="flex min-h-screen flex-col items-center bg-muted/30 px-6 py-10 sm:py-15">
      <div className="w-full max-w-5xl">
        <header className="mb-10">
          <p className="mb-3 text-xs tracking-[0.2em] text-muted-foreground uppercase">
            Directory Service
          </p>
          <div className="flex items-center gap-4">
            <span className="flex size-12 shrink-0 items-center justify-center bg-yellow-500/10 text-yellow-700 dark:text-yellow-400">
              <TreeStructureIcon
                size={26}
                weight="duotone"
                aria-hidden="true"
              />
            </span>
            <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">
              Оргструктура
            </h1>
          </div>
          <p className="mt-4 max-w-xl text-sm leading-relaxed text-muted-foreground">
            Выберите подразделение в дереве, чтобы посмотреть информацию о нём и
            список должностей.
          </p>
        </header>

        <Suspense fallback={<p role="status">Загружаем оргструктуру…</p>}>
          <OrganizationStructureSection />
        </Suspense>
      </div>
    </main>
  );
}
