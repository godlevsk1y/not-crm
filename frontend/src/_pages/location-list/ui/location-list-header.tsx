import { MapPinIcon } from "@phosphor-icons/react/ssr";

export default function LocationListHeader() {
  return (
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
        Места, где работает наша команда. Адреса офисов и рабочих пространств,
        связанных с подразделениями организации.
      </p>
    </header>
  );
}
