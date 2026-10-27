export type { DepartmentListItemDto } from "./model/types";

export { DepartmentListItemCard } from "./ui/department-list-item-card";
export { DepartmentListItemCardSkeleton } from "./ui/department-list-item-card-skeleton";

export {
  departmentsApi,
  departmentsQueryOptions,
  type GetDepartmentListRequest,
} from "./api/api";
export {
  useDepartmentList,
  type UseDepartmentListProps,
  type DepartmentListState,
} from "./api/hooks/use-department-list";
