export type {
  DepartmentDto,
  DepartmentListItemDto,
  DepartmentNodeDto,
  DepartmentDescendantDto,
  TransferredDepartmentDto,
} from "./model/types";

export { DepartmentListItemCard } from "./ui/department-list-item-card";
export { DepartmentListItemCardSkeleton } from "./ui/department-list-item-card-skeleton";

export {
  departmentsApi,
  departmentsQueryOptions,
  type GetDepartmentListRequest,
  type GetRootDepartmentsRequest,
  type GetImmediateChildrenRequest,
  type UpdateDepartmentRequest,
  type CreateDepartmentRequest,
  type DeleteDepartmentRequest,
  type TransferDepartmentRequest,
} from "./api/api";

export { departmentSchema, type DepartmentFormValues } from "./model/validation";
