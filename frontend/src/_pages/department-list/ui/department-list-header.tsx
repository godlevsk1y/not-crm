import { TreeStructureIcon } from "@phosphor-icons/react/ssr";

import { CreateDepartmentDialog } from "./create-department-dialog";

export default function DepartmentListHeader() {
  return (
    <header className="mb-10">
      <p className="mb-3 text-xs tracking-[0.2em] text-muted-foreground uppercase">
        Directory Service
      </p>
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <span className="flex size-12 shrink-0 items-center justify-center bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
            <TreeStructureIcon size={26} weight="duotone" aria-hidden="true" />
          </span>
          <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">
            Подразделения
          </h1>
        </div>
        <CreateDepartmentDialog />
      </div>
      <p className="mt-4 max-w-xl text-sm leading-relaxed text-muted-foreground">
        Структура нашей команды. Подразделения организации, их названия и место
        в общей иерархии.
      </p>
    </header>
  );
}
