import type { DepartmentListItemDto } from "@/entities/department";

export type DepartmentSelectOption = Pick<DepartmentListItemDto, "id" | "name">;

export type DepartmentSelectProps = {
  id?: string;
  label?: string;
  placeholder?: string;
  disabled?: boolean;
  excludeIds?: string[];
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
