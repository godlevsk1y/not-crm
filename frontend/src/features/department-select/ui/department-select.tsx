"use client";

import { RefCallback, useCallback, useId, useRef, useState } from "react";
import {
  ArrowClockwiseIcon,
  CaretUpDownIcon,
  MagnifyingGlassIcon,
  TreeStructureIcon,
  WarningCircleIcon,
  XIcon,
} from "@phosphor-icons/react";

import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import { Checkbox } from "@/shared/ui/checkbox";
import {
  Command,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/shared/ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTitle,
  PopoverTrigger,
} from "@/shared/ui/popover";
import { Spinner } from "@/shared/ui/spinner";
import type {
  DepartmentSelectOption,
  DepartmentSelectProps,
} from "../model/types";
import { useDebounce } from "use-debounce";
import { useInfiniteDepartmentSelect } from "../api/use-infinite-department-select";

const PAGE_SIZE = 10;

const activityFilters = [
  { value: "all", label: "Все" },
  { value: "active", label: "Активные" },
  { value: "inactive", label: "Архивные" },
] as const;

export function DepartmentSelect(props: DepartmentSelectProps) {
  const generatedId = useId();
  const id = props.id ?? generatedId;
  const {
    label = "Подразделения",
    placeholder = "Выберите подразделение",
    disabled = false,
  } = props;
  const [open, setOpen] = useState(false);
  const searchRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const [activity, setActivity] = useState<"all" | "active" | "inactive">(
    "all",
  );

  const [search, setSearch] = useState("");
  const [debouncedSearch] = useDebounce(search, 1000);

  const {
    items: departments,
    isPending,
    isError,
    refetch,
    isFetchingNextPage,
    fetchNextPage,
    hasNextPage,
  } = useInfiniteDepartmentSelect({
    search: debouncedSearch,
    sortBy: "createdAt",
    sortDirection: "desc",
    isActive: activity === "all" ? undefined : activity === "active",
    excludeIds: props.excludeIds,
    pageSize: PAGE_SIZE,
  });

  const cursorRef: RefCallback<HTMLDivElement> = useCallback(
    (el) => {
      const observer = new IntersectionObserver(
        (entries) => {
          if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
            fetchNextPage();
          }
        },
        { root: listRef.current, threshold: 0.8 },
      );

      if (el) {
        observer.observe(el);

        return () => observer.disconnect();
      }
    },
    [fetchNextPage, hasNextPage, isFetchingNextPage],
  );

  let selected: DepartmentSelectOption[] = [];

  if (props.mode === "multiple") {
    selected = props.value;
  } else if (props.value !== null) {
    selected = [props.value];
  }

  function selectDepartment(department: DepartmentSelectOption) {
    if (props.mode === "multiple") {
      if (selected.some((item) => item.id === department.id)) {
        removeDepartment(department.id);
      } else {
        props.onChange([...props.value, department]);
      }
    } else {
      props.onChange(department);
      setOpen(false);
    }
  }

  function removeDepartment(id: string) {
    if (props.mode === "multiple") {
      props.onChange(props.value.filter((department) => department.id !== id));
    } else {
      props.onChange(null);
    }
  }

  return (
    <div className="grid min-w-0 gap-2">
      <label htmlFor={id} className="text-xs font-medium">
        {label}
      </label>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger
          id={id}
          disabled={disabled}
          render={
            <Button
              type="button"
              variant="outline"
              className="w-full justify-between font-normal"
            />
          }
        >
          <span className="flex min-w-0 items-center gap-2">
            <TreeStructureIcon
              className="shrink-0 text-muted-foreground"
              aria-hidden="true"
            />
            <span className="truncate">
              {selected.length > 0 ? (
                props.mode === "single" ? (
                  selected[0].name
                ) : (
                  `Выбрано: ${selected.length}`
                )
              ) : (
                <span className="text-muted-foreground">{placeholder}</span>
              )}
            </span>
          </span>
          <CaretUpDownIcon
            className="text-muted-foreground"
            aria-hidden="true"
          />
        </PopoverTrigger>
        <PopoverContent
          initialFocus={searchRef}
          align="start"
          className="w-(--anchor-width) max-w-[calc(100vw-2rem)] gap-0 p-0"
        >
          <PopoverTitle className="sr-only">{label}</PopoverTitle>
          <Command shouldFilter={false} label={label}>
            <CommandInput
              ref={searchRef}
              aria-label="Поиск подразделений"
              placeholder="Найти подразделение…"
              value={search}
              onValueChange={setSearch}
            />
            <div
              className="flex gap-1 border-b p-2"
              role="group"
              aria-label="Статус подразделений"
            >
              {activityFilters.map((filter) => (
                <Button
                  key={filter.value}
                  type="button"
                  size="xs"
                  variant={activity === filter.value ? "secondary" : "ghost"}
                  aria-pressed={activity === filter.value}
                  onClick={() => setActivity(filter.value)}
                >
                  {filter.label}
                </Button>
              ))}
            </div>
            <CommandList ref={listRef} aria-label="Подразделения" className="max-h-64">
              {isPending ? (
                <div
                  role="status"
                  className="flex min-h-40 items-center justify-center gap-2 p-4 text-muted-foreground"
                >
                  <Spinner />
                  Загрузка подразделений…
                </div>
              ) : isError ? (
                <div
                  role="alert"
                  className="flex min-h-40 flex-col items-center justify-center gap-3 p-4 text-center"
                >
                  <WarningCircleIcon
                    className="size-6 text-destructive"
                    aria-hidden="true"
                  />
                  <p className="text-xs text-muted-foreground">
                    Не удалось загрузить подразделения
                  </p>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => refetch()}
                  >
                    <ArrowClockwiseIcon aria-hidden="true" />
                    Повторить
                  </Button>
                </div>
              ) : departments?.length === 0 ? (
                <div
                  role="status"
                  className="flex min-h-40 flex-col items-center justify-center gap-2 p-4 text-center"
                >
                  <MagnifyingGlassIcon
                    className="size-6 text-muted-foreground"
                    aria-hidden="true"
                  />
                  <p className="text-xs font-medium">
                    Подразделения не найдены
                  </p>
                  <p className="text-xs text-muted-foreground">
                    Измените запрос или фильтр статуса.
                  </p>
                </div>
              ) : (
                departments?.map((department) => {
                  const isSelected = selected.some(
                    (item) => item.id === department.id,
                  );

                  return (
                    <CommandItem
                      key={department.id}
                      value={department.id}
                      data-checked={isSelected}
                      onSelect={() => selectDepartment(department)}
                      className="min-h-10 cursor-pointer"
                    >
                      {props.mode === "multiple" ? (
                        <Checkbox
                          checked={isSelected}
                          readOnly
                          tabIndex={-1}
                          aria-hidden="true"
                          className="pointer-events-none"
                        />
                      ) : (
                        <TreeStructureIcon
                          className="text-muted-foreground"
                          aria-hidden="true"
                        />
                      )}
                      <span className="min-w-0 flex-1 wrap-break-words">
                        {department.name}
                      </span>
                      {!department.isActive && (
                        <Badge variant="outline">Архивное</Badge>
                      )}
                    </CommandItem>
                  );
                })
              )}
              {hasNextPage && (
                <div ref={cursorRef} className="flex justify-center py-8">
                  {isFetchingNextPage && <Spinner />}
                </div>
              )}
            </CommandList>
          </Command>
          <div className="border-t px-3 py-2 text-xs text-muted-foreground">
            {props.mode === "multiple"
              ? "Можно выбрать несколько подразделений"
              : "Можно выбрать одно подразделение"}
          </div>
        </PopoverContent>
      </Popover>

      {selected.length > 0 && (
        <ul
          aria-label="Выбранные подразделения"
          className="flex flex-wrap gap-1.5"
        >
          {selected.map((department) => (
            <li key={department.id} className="min-w-0 max-w-full">
              <Badge
                variant="secondary"
                className="h-auto max-w-full gap-1 py-1 pr-1"
              >
                <span className="truncate">{department.name}</span>
                <button
                  type="button"
                  disabled={disabled}
                  aria-label={`Убрать подразделение «${department.name}»`}
                  className="flex size-4 shrink-0 items-center justify-center text-muted-foreground outline-none hover:bg-muted hover:text-foreground focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50"
                  onClick={() => removeDepartment(department.id)}
                >
                  <XIcon className="size-3" aria-hidden="true" />
                </button>
              </Badge>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
