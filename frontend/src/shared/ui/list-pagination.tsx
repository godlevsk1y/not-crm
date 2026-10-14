"use client";

import { CaretLeftIcon, CaretRightIcon } from "@phosphor-icons/react";

import { Button } from "@/shared/ui/button";
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
} from "@/shared/ui/pagination";

export type ListPaginationProps = {
  page: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  disabled?: boolean;
};

export function ListPagination({
  page,
  totalCount,
  pageSize,
  onPageChange,
  disabled = false,
}: ListPaginationProps) {
  const pageCount = Math.ceil(totalCount / pageSize);

  if (pageCount <= 1) {
    return null;
  }

  const start = page <= 4 ? 1 : Math.min(page - 1, pageCount - 4);
  const end = page >= pageCount - 3 ? pageCount : Math.max(page + 1, 5);
  const pages: (number | "start-ellipsis" | "end-ellipsis")[] =
    pageCount <= 7
      ? Array.from({ length: pageCount }, (_, index) => index + 1)
      : [
          ...(start > 1 ? [1, "start-ellipsis" as const] : []),
          ...Array.from({ length: end - start + 1 }, (_, index) => start + index),
          ...(end < pageCount ? ["end-ellipsis" as const, pageCount] : []),
        ];

  return (
    <div className="mt-8 flex flex-col items-center gap-4 border-t pt-5 sm:flex-row sm:flex-wrap sm:justify-between">
      <p className="hidden shrink-0 text-xs text-muted-foreground sm:block">
        Страница <span className="font-medium text-foreground">{page}</span> из{" "}
        <span className="font-medium text-foreground">{pageCount}</span>
      </p>

      <Pagination
        aria-label="Навигация по страницам"
        className="mx-0 w-auto"
      >
        <PaginationContent className="gap-1">
          <PaginationItem className="mr-1">
            <Button
              type="button"
              variant="outline"
              size="lg"
              className="gap-2 disabled:opacity-40"
              aria-label="Предыдущая страница"
              disabled={disabled || page <= 1}
              onClick={() => onPageChange(page - 1)}
            >
              <CaretLeftIcon aria-hidden="true" />
              <span className="hidden lg:inline">Назад</span>
            </Button>
          </PaginationItem>

          <PaginationItem className="sm:hidden">
            <span className="flex h-9 min-w-24 items-center justify-center gap-2 text-xs tabular-nums">
              <span className="font-semibold">{page}</span>
              <span className="text-muted-foreground">/ {pageCount}</span>
            </span>
          </PaginationItem>

          {pages.map((item) => (
            <PaginationItem key={item} className="hidden sm:block">
              {typeof item === "number" ? (
                <Button
                  type="button"
                  variant={item === page ? "default" : "ghost"}
                  size="icon-lg"
                  className="tabular-nums"
                  aria-label={`Страница ${item}`}
                  aria-current={item === page ? "page" : undefined}
                  disabled={disabled}
                  onClick={() => {
                    if (item !== page) {
                      onPageChange(item);
                    }
                  }}
                >
                  {item}
                </Button>
              ) : (
                <PaginationEllipsis className="size-9 text-muted-foreground" />
              )}
            </PaginationItem>
          ))}

          <PaginationItem className="ml-1">
            <Button
              type="button"
              variant="outline"
              size="lg"
              className="gap-2 disabled:opacity-40"
              aria-label="Следующая страница"
              disabled={disabled || page >= pageCount}
              onClick={() => onPageChange(page + 1)}
            >
              <span className="hidden lg:inline">Далее</span>
              <CaretRightIcon aria-hidden="true" />
            </Button>
          </PaginationItem>
        </PaginationContent>
      </Pagination>
    </div>
  );
}
