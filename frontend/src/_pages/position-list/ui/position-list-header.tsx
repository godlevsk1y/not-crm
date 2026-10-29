import { BriefcaseIcon } from "@phosphor-icons/react/ssr";

import { CreatePositionDialog } from "./create-position-dialog";

export default function PositionListHeader() {
  return (
    <header className="mb-10">
      <p className="mb-3 text-xs tracking-[0.2em] text-muted-foreground uppercase">
        Directory Service
      </p>
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <span className="flex size-12 shrink-0 items-center justify-center bg-violet-500/10 text-violet-600 dark:text-violet-400">
            <BriefcaseIcon size={26} weight="duotone" aria-hidden="true" />
          </span>
          <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">
            Должности
          </h1>
        </div>
        <CreatePositionDialog />
      </div>
      <p className="mt-4 max-w-xl text-sm leading-relaxed text-muted-foreground">
        Справочник должностей нашей команды. Названия должностей, связанных
        с подразделениями организации.
      </p>
    </header>
  );
}
