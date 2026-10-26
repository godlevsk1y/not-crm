import type { DepartmentSelectOption } from "./types";

// Temporary data for the layout preview. Replace with API results when integrating.
export const mockDepartments: DepartmentSelectOption[] = [
  { id: "engineering", name: "Разработка", isActive: true },
  { id: "design", name: "Дизайн", isActive: true },
  { id: "product", name: "Управление продуктом", isActive: true },
  { id: "qa", name: "Тестирование", isActive: true },
  { id: "analytics", name: "Аналитика", isActive: true },
  { id: "sales", name: "Продажи", isActive: true },
  { id: "marketing", name: "Маркетинг", isActive: true },
  { id: "support", name: "Поддержка клиентов", isActive: true },
  { id: "hr", name: "Работа с персоналом", isActive: true },
  { id: "finance", name: "Финансы", isActive: true },
  { id: "archive", name: "Региональное представительство", isActive: false },
  { id: "legacy", name: "Сопровождение прежних продуктов", isActive: false },
];
