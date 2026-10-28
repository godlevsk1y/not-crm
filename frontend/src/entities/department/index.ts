export type {
  DepartmentDto,
  DepartmentListItemDto,
  DepartmentNodeDto,
} from "./model/types";

export { DepartmentListItemCard } from "./ui/department-list-item-card";
export { DepartmentListItemCardSkeleton } from "./ui/department-list-item-card-skeleton";

export {
  departmentsApi,
  departmentsQueryOptions,
  type GetDepartmentListRequest,
  type GetRootDepartmentsRequest,
  type GetImmediateChildrenRequest,
} from "./api/api";
