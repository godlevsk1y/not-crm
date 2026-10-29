import { RestoreDepartmentButton } from "@/features/department-actions";
import {
  DepartmentListItemCard,
  type DepartmentListItemDto,
} from "@/entities/department";

type DepartmentListProps = {
  departments: DepartmentListItemDto[];
  onCardUpdate: (department: DepartmentListItemDto) => void;
  onCardDelete: (department: DepartmentListItemDto) => void;
};

export function DepartmentList({
  departments,
  onCardUpdate,
  onCardDelete,
}: DepartmentListProps) {
  return (
    <ul className="grid gap-5 grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
      {departments.map((department) => (
        <li key={department.id} className="min-w-0">
          <DepartmentListItemCard
            department={department}
            restoreAction={
              !department.isActive ? (
                <RestoreDepartmentButton id={department.id} />
              ) : undefined
            }
            onUpdate={department.isActive ? () => onCardUpdate(department) : undefined}
            onDelete={department.isActive ? () => onCardDelete(department) : undefined}
          />
        </li>
      ))}
    </ul>
  );
}
