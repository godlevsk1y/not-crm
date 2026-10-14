import type { DepartmentListState } from "../model/use-department-list";

import { DepartmentList } from "./department-list";
import {
  DepartmentListLoading,
  DepartmentListError,
  DepartmentListEmpty,
} from "./department-list-states";

interface DepartmentListContentProps {
  state: DepartmentListState;
  onRetry: () => void;
}

export function DepartmentListContent({
  state,
  onRetry,
}: DepartmentListContentProps) {
  switch (state.status) {
    case "loading":
      return <DepartmentListLoading />;

    case "error":
      return <DepartmentListError message={state.message} onRetry={onRetry} />;

    case "success":
      if (state.departments.length === 0) {
        return <DepartmentListEmpty />;
      }

      return <DepartmentList departments={state.departments} />;
  }
}
