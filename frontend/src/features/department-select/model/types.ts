import type { DepartmentListItemDto } from "@/entities/department";

export type DepartmentSelectOption = Pick<
  DepartmentListItemDto,
  "id" | "name"
> & {
  isActive: boolean;
};

export type DepartmentSelectStatus = "ready" | "loading" | "error";

export type DepartmentSelectProps = {
  id?: string;
  label?: string;
  placeholder?: string;
  disabled?: boolean;
  excludeIds?: string[];
  departments?: DepartmentSelectOption[];
  status?: DepartmentSelectStatus;
  onRetry?: () => void;
} & (
  | {
      mode: "single";
      value: DepartmentSelectOption | null;
      onChange: (value: DepartmentSelectOption | null) => void;
    }
  | {
      mode: "multiple";
      value: DepartmentSelectOption[];
      onChange: (value: DepartmentSelectOption[]) => void;
    }
);
