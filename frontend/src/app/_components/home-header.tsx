export default function HomeHeader() {
  return (
    <header className="mb-10">
      <p className="mb-3 text-xs tracking-[0.2em] text-muted-foreground uppercase">
        Справочник организации
      </p>
      <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">
        Directory Service
      </h1>
      <p className="mt-4 max-w-xl text-sm leading-relaxed text-muted-foreground">
        Структура организации, места работы и должности в одном справочнике.
        Подразделения объединяют эти данные и связывают их между собой.
      </p>
    </header>
  );
}
