import {
  BriefcaseIcon,
  MapPinIcon,
  TreeStructureIcon,
} from "@phosphor-icons/react/ssr";

export default function HomeRelationships() {
  return (
    <section
      aria-labelledby="relationships-heading"
      className="mt-10 grid gap-8 border-t pt-8 md:grid-cols-[1fr_1.5fr] md:gap-12"
    >
      <div>
        <p className="mb-3 flex items-center gap-2 text-xs tracking-wider text-muted-foreground uppercase">
          <TreeStructureIcon size={16} aria-hidden="true" />
          Единая структура
        </p>
        <h2
          id="relationships-heading"
          className="font-heading text-lg font-semibold tracking-tight"
        >
          Как связаны справочники
        </h2>
        <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
          В основе — подразделение. У него может быть родительское подразделение,
          а также связанные локации и должности.
        </p>
      </div>
      <dl className="grid gap-6">
        <div className="flex items-start gap-4">
          <span className="flex size-9 shrink-0 items-center justify-center bg-sky-500/10 text-sky-600 dark:text-sky-400">
            <MapPinIcon size={20} weight="duotone" aria-hidden="true" />
          </span>
          <div>
            <dt className="text-sm font-medium">Где работает подразделение</dt>
            <dd className="mt-1 text-sm leading-relaxed text-muted-foreground">
              С подразделением можно связать несколько локаций и выделить основную.
              Каждая локация хранит название и адрес.
            </dd>
          </div>
        </div>
        <div className="flex items-start gap-4">
          <span className="flex size-9 shrink-0 items-center justify-center bg-violet-500/10 text-violet-600 dark:text-violet-400">
            <BriefcaseIcon size={20} weight="duotone" aria-hidden="true" />
          </span>
          <div>
            <dt className="text-sm font-medium">Какие должности в подразделении</dt>
            <dd className="mt-1 text-sm leading-relaxed text-muted-foreground">
              Подразделение может включать несколько должностей. Одна и та же
              должность может относиться к разным подразделениям.
            </dd>
          </div>
        </div>
      </dl>
    </section>
  );
}
