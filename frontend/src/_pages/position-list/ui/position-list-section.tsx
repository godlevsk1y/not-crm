import type { PositionListItemDto } from "@/entities/position";

import { PositionList } from "./position-list";

// Статические примеры для верстки страницы.
const positions: PositionListItemDto[] = [
  { id: "1", name: "Генеральный директор", createdAt: "2026-10-01T00:00:00Z" },
  {
    id: "2",
    name: "Руководитель отдела разработки",
    createdAt: "2026-10-02T00:00:00Z",
  },
  { id: "3", name: "Разработчик", createdAt: "2026-10-03T00:00:00Z" },
  { id: "4", name: "Дизайнер", createdAt: "2026-10-04T00:00:00Z" },
  { id: "5", name: "Менеджер проектов", createdAt: "2026-10-05T00:00:00Z" },
  {
    id: "6",
    name: "Специалист по подбору персонала",
    createdAt: "2026-10-06T00:00:00Z",
  },
];

export function PositionListSection() {
  return (
    <section aria-label="Должности организации">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b pb-4">
        <h2 className="text-sm font-medium">Все должности</h2>
        <p className="text-xs text-muted-foreground">
          Должностей: {positions.length}
        </p>
      </div>

      <PositionList positions={positions} />
    </section>
  );
}
