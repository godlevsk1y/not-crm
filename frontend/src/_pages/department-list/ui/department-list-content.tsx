import { useState } from "react";
import type { DepartmentListItemDto } from "@/entities/department";
import type { DepartmentListState } from "../model/use-department-list";
import { DepartmentList } from "./department-list";
import { UpdateDepartmentDialog } from "./update-department-dialog";
import { DeleteDepartmentDialog } from "./delete-department-dialog";
import {
  DepartmentListLoading,
  DepartmentListError,
  DepartmentListEmpty,
} from "./department-list-states";

interface DepartmentListContentProps {
  state: DepartmentListState;
  onRetry: () => void;
  isActive: boolean | undefined;
}

export function DepartmentListContent({
  state,
  onRetry,
  isActive,
}: DepartmentListContentProps) {
  const [updatingDepartment, setUpdatingDepartment] =
    useState<DepartmentListItemDto | null>(null);
  const [deletingDepartment, setDeletingDepartment] =
    useState<DepartmentListItemDto | null>(null);

  switch (state.status) {
    case "loading":
      return <DepartmentListLoading />;

    case "error":
      return <DepartmentListError message={state.message} onRetry={onRetry} />;

    case "success":
      if (state.departments.length === 0) {
        return <DepartmentListEmpty isActive={isActive} />;
      }

      return (
        <>
          <DepartmentList
            departments={state.departments}
            onCardUpdate={setUpdatingDepartment}
            onCardDelete={setDeletingDepartment}
          />
          {updatingDepartment && (
            <UpdateDepartmentDialog
              department={updatingDepartment}
              onClose={() => setUpdatingDepartment(null)}
            />
          )}
          {deletingDepartment && (
            <DeleteDepartmentDialog
              department={deletingDepartment}
              onClose={() => setDeletingDepartment(null)}
            />
          )}
        </>
      );
  }
}
