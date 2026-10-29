"use client";

import { RefCallback, useCallback, useId, useRef, useState } from "react";
import {
  ArrowClockwiseIcon,
  CaretUpDownIcon,
  MagnifyingGlassIcon,
  MapPinIcon,
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
  LocationSelectOption,
  LocationSelectProps,
} from "../model/types";
import { useDebounce } from "use-debounce";
import { useInfiniteLocationSelect } from "../api/use-infinite-location-select";

const PAGE_SIZE = 10;

export function LocationSelect(props: LocationSelectProps) {
  const generatedId = useId();
  const id = props.id ?? generatedId;
  const {
    label = "Локации",
    placeholder = "Выберите локацию",
    disabled = false,
  } = props;
  const [open, setOpen] = useState(false);
  const searchRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLDivElement>(null);

  const [search, setSearch] = useState("");
  const [debouncedSearch] = useDebounce(search, 1000);

  const {
    items: locations,
    isPending,
    isError,
    refetch,
    isFetchNextPageError,
    isFetchingNextPage,
    fetchNextPage,
    hasNextPage,
  } = useInfiniteLocationSelect(
    {
      search: debouncedSearch,
      sortBy: "createdAt",
      sortDirection: "desc",
      isActive: true,
      pageSize: PAGE_SIZE,
    },
    open && !disabled,
  );

  const visibleLocations = locations?.filter(
    (location) => !props.excludeIds?.includes(location.id),
  );

  const cursorRef: RefCallback<HTMLDivElement> = useCallback(
    (el) => {
      const observer = new IntersectionObserver(
        (entries) => {
          if (
            entries[0].isIntersecting &&
            hasNextPage &&
            !isFetchingNextPage &&
            !isError
          ) {
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
    [fetchNextPage, hasNextPage, isFetchingNextPage, isError],
  );

  let selected: LocationSelectOption[] = [];

  if (props.mode === "multiple") {
    selected = props.value;
  } else if (props.value !== null) {
    selected = [props.value];
  }

  function selectLocation(location: LocationSelectOption) {
    if (disabled) return;
    if (props.mode === "multiple") {
      if (selected.some((item) => item.id === location.id)) {
        removeLocation(location.id);
      } else {
        props.onChange([...props.value, location]);
      }
    } else {
      props.onChange(location);
      setOpen(false);
    }
  }

  function removeLocation(id: string) {
    if (disabled) return;
    if (props.mode === "multiple") {
      props.onChange(props.value.filter((location) => location.id !== id));
    } else {
      props.onChange(null);
    }
  }

  return (
    <div className="grid min-w-0 gap-2">
      <label htmlFor={id} className="text-xs font-medium">
        {label}
      </label>
      <Popover open={open && !disabled} onOpenChange={setOpen}>
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
            <MapPinIcon
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
              disabled={disabled}
              aria-label="Поиск локаций"
              placeholder="Найти локацию…"
              value={search}
              onValueChange={setSearch}
            />
            <CommandList ref={listRef} aria-label="Локации" className="max-h-64">
              {isPending ? (
                <div
                  role="status"
                  className="flex min-h-40 items-center justify-center gap-2 p-4 text-muted-foreground"
                >
                  <Spinner />
                  Загрузка локаций…
                </div>
              ) : isError && !isFetchNextPageError ? (
                <div
                  role="alert"
                  className="flex min-h-40 flex-col items-center justify-center gap-3 p-4 text-center"
                >
                  <WarningCircleIcon
                    className="size-6 text-destructive"
                    aria-hidden="true"
                  />
                  <p className="text-xs text-muted-foreground">
                    Не удалось загрузить локации
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
              ) : visibleLocations?.length === 0 ? (
                <div
                  role="status"
                  className="flex min-h-40 flex-col items-center justify-center gap-2 p-4 text-center"
                >
                  <MagnifyingGlassIcon
                    className="size-6 text-muted-foreground"
                    aria-hidden="true"
                  />
                  <p className="text-xs font-medium">
                    Локации не найдены
                  </p>
                  <p className="text-xs text-muted-foreground">
                    Измените поисковый запрос.
                  </p>
                </div>
              ) : (
                visibleLocations?.map((location) => {
                  const isSelected = selected.some(
                    (item) => item.id === location.id,
                  );

                  return (
                    <CommandItem
                      key={location.id}
                      value={location.id}
                      data-checked={isSelected}
                      onSelect={() => selectLocation(location)}
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
                        <MapPinIcon
                          className="text-muted-foreground"
                          aria-hidden="true"
                        />
                      )}
                      <span className="min-w-0 flex-1 wrap-break-words">
                        {location.name}
                      </span>
                    </CommandItem>
                  );
                })
              )}
              {isFetchNextPageError ? (
                <div role="alert" className="grid justify-items-center gap-2 p-4">
                  <p className="text-xs text-muted-foreground">
                    Не удалось загрузить следующие локации
                  </p>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => fetchNextPage()}
                  >
                    <ArrowClockwiseIcon aria-hidden="true" />
                    Повторить
                  </Button>
                </div>
              ) : hasNextPage ? (
                <div ref={cursorRef} className="flex justify-center p-2">
                  {isFetchingNextPage && <Spinner />}
                </div>
              ) : null}
            </CommandList>
          </Command>
          <div className="border-t px-3 py-2 text-xs text-muted-foreground">
            {props.mode === "multiple"
              ? "Можно выбрать несколько локаций"
              : "Можно выбрать одну локацию"}
          </div>
        </PopoverContent>
      </Popover>

      {selected.length > 0 && (
        <ul
          aria-label="Выбранные локации"
          className="flex flex-wrap gap-1.5"
        >
          {selected.map((location) => (
            <li key={location.id} className="min-w-0 max-w-full">
              <Badge
                variant="secondary"
                className="h-auto max-w-full gap-1 py-1 pr-1"
              >
                <span className="truncate">{location.name}</span>
                <button
                  type="button"
                  disabled={disabled}
                  aria-label={`Убрать локацию «${location.name}»`}
                  className="flex size-4 shrink-0 items-center justify-center text-muted-foreground outline-none hover:bg-muted hover:text-foreground focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50"
                  onClick={() => removeLocation(location.id)}
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
