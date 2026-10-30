export const ROUTES = {
  HOME: "/",
  ORGANIZATION_STRUCTURE: "/organization-structure",
  DEPARTMENTS: "/departments",
  LOCATIONS: "/locations",
  POSITIONS: "/positions",
} as const;

export function getDepartmentStructureHref(id: string) {
  return `${ROUTES.ORGANIZATION_STRUCTURE}?${new URLSearchParams({ departmentId: id })}`;
}
