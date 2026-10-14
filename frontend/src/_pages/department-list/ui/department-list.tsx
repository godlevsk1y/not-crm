import {
  DepartmentListItemCard,
  type DepartmentListItemDto,
} from "@/entities/department";

type DepartmentListProps = {
  departments: DepartmentListItemDto[];
};

export function DepartmentList({ departments }: DepartmentListProps) {
  return (
    <ul className="grid gap-5 grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
      {departments.map((department) => (
        <li key={department.id} className="min-w-0">
          <DepartmentListItemCard department={department} />
        </li>
      ))}
    </ul>
  );
}
