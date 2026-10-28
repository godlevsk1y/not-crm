import { useId } from "react";
import {
  CaretRightIcon,
  FolderIcon,
  FolderOpenIcon,
} from "@phosphor-icons/react";
import { cn } from "cn";

import type { DepartmentNodeDto } from "@/entities/department";
import { Button } from "@/shared/ui/button";
import { Skeleton } from "@/shared/ui/skeleton";

import { useTreeStore } from "../model/tree-store";
import { useInfiniteImmediateChildren } from "../model/use-infinite-immediate-children";
import { useInfiniteRootDepartments } from "../model/use-infinite-root-departments";
import { LoadMore, QueryError } from "./query-controls";

const PAGE_SIZE = 20;

type DepartmentTreeProps = {
  selectedId?: string;
  onSelect: (department: DepartmentNodeDto) => void;
};

export function DepartmentTree(props: DepartmentTreeProps) {
  const query = useInfiniteRootDepartments({ pageSize: PAGE_SIZE });

  return (
    <ul aria-label="Корневые подразделения">
      <DepartmentTreeItems {...props} query={query} />
    </ul>
  );
}

function DepartmentTreeItems({
  query,
  ...props
}: DepartmentTreeProps & {
  query: ReturnType<typeof useInfiniteRootDepartments>;
}) {
  const {
    items,
    isPending,
    isError,
    refetch,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
  } = query;

  if (isPending) {
    return (
      <li role="status" className="space-y-3 p-3">
        <span className="sr-only">Загружаем подразделения…</span>
        {Array.from({ length: 3 }, (_, index) => (
          <Skeleton
            key={index}
            className="h-5 w-full motion-reduce:animate-none"
            aria-hidden="true"
          />
        ))}
      </li>
    );
  }

  return (
    <>
      {items?.map((department) => (
        <DepartmentTreeNode
          key={department.id}
          department={department}
          {...props}
        />
      ))}

      {isError ? (
        <li>
          <QueryError
            message="Не удалось загрузить подразделения."
            onRetry={() => void refetch()}
          />
        </li>
      ) : !items?.length ? (
        <li role="status" className="p-3 text-xs text-muted-foreground">
          Подразделений пока нет.
        </li>
      ) : null}

      {hasNextPage && !isError && (
        <li className="px-3">
          <LoadMore
            isFetching={isFetchingNextPage}
            onLoadMore={() => void fetchNextPage()}
          />
        </li>
      )}
    </>
  );
}

function DepartmentTreeNode({
  department,
  selectedId,
  onSelect,
}: DepartmentTreeProps & { department: DepartmentNodeDto }) {
  const childrenId = useId();
  const isExpanded = useTreeStore((state) =>
    state.expandedIds.has(department.id),
  );
  const expandNode = useTreeStore((state) => state.expandNode);
  const collapseNode = useTreeStore((state) => state.collapseNode);
  const isSelected = selectedId === department.id;
  const Folder = isExpanded ? FolderOpenIcon : FolderIcon;

  return (
    <li>
      <div
        className={cn(
          "flex items-center hover:bg-muted/70",
          isSelected && "bg-emerald-500/10 hover:bg-emerald-500/15",
        )}
      >
        {department.hasChildren ? (
          <Button
            variant="ghost"
            size="icon-sm"
            className="shrink-0"
            aria-label={`${isExpanded ? "Свернуть" : "Развернуть"} ${department.name}`}
            aria-expanded={isExpanded}
            aria-controls={isExpanded ? childrenId : undefined}
            onClick={() =>
              isExpanded
                ? collapseNode(department.id)
                : expandNode(department.id)
            }
          >
            <CaretRightIcon
              className={cn(
                "transition-transform motion-reduce:transition-none",
                isExpanded && "rotate-90",
              )}
              aria-hidden="true"
            />
          </Button>
        ) : (
          <span className="w-7 shrink-0" aria-hidden="true" />
        )}
        <button
          type="button"
          aria-current={isSelected ? "true" : undefined}
          onClick={() => onSelect(department)}
          title={department.name}
          className={cn(
            "flex min-w-0 flex-1 items-center gap-2 py-2 pr-2 text-left text-sm outline-none focus-visible:ring-1 focus-visible:ring-ring",
            isSelected && "font-medium text-emerald-700 dark:text-emerald-300",
          )}
        >
          <Folder
            className="size-4 shrink-0 text-emerald-600 dark:text-emerald-400"
            weight="duotone"
            aria-hidden="true"
          />
          <span className="truncate">{department.name}</span>
          {department.hasChildren && (
            <span className="ml-auto text-xs text-muted-foreground">
              {department.childCount}
            </span>
          )}
        </button>
      </div>

      {department.hasChildren && isExpanded && (
        <ul
          id={childrenId}
          aria-label={`Подразделения: ${department.name}`}
          className="ml-3 border-l pl-2"
        >
          <DepartmentTreeChildren
            id={department.id}
            selectedId={selectedId}
            onSelect={onSelect}
          />
        </ul>
      )}
    </li>
  );
}

function DepartmentTreeChildren({
  id,
  ...props
}: DepartmentTreeProps & { id: string }) {
  const query = useInfiniteImmediateChildren({ id, pageSize: PAGE_SIZE });

  return <DepartmentTreeItems {...props} query={query} />;
}
