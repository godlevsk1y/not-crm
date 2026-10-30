"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import { useDebounce } from "use-debounce";
import {
  BriefcaseIcon,
  MagnifyingGlassIcon,
  MapPinIcon,
  TreeStructureIcon,
} from "@phosphor-icons/react";

import { getDepartmentStructureHref } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import {
  Command,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/shared/ui/command";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/shared/ui/dialog";
import { Spinner } from "@/shared/ui/spinner";
import {
  SEARCH_PAGE_SIZE,
  searchDirectory,
  searchDirectoryPage,
  type SearchPage,
} from "../api/search";

const groups = [
  {
    key: "departments",
    title: "Подразделения",
    icon: TreeStructureIcon,
    accent: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
  },
  {
    key: "locations",
    title: "Локации",
    icon: MapPinIcon,
    accent: "bg-sky-500/10 text-sky-600 dark:text-sky-400",
  },
  {
    key: "positions",
    title: "Должности",
    icon: BriefcaseIcon,
    accent: "bg-violet-500/10 text-violet-600 dark:text-violet-400",
  },
] as const;

export function GlobalSearch() {
  const [open, setOpen] = useState(false);
  const returnFocus = useRef<HTMLElement | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        if (!open) {
          returnFocus.current =
            document.activeElement instanceof HTMLElement
              ? document.activeElement
              : triggerRef.current;
          setOpen(true);
        }
      }
    }
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [open]);

  return (
    <>
      <Button
        ref={triggerRef}
        variant="outline"
        className="ml-auto gap-2 text-muted-foreground sm:w-64 sm:justify-start"
        aria-label="Поиск по справочникам"
        aria-haspopup="dialog"
        aria-expanded={open}
        onClick={() => {
          returnFocus.current = triggerRef.current;
          setOpen(true);
        }}
      >
        <MagnifyingGlassIcon aria-hidden="true" />
        <span className="hidden sm:inline">Поиск…</span>
        <kbd className="ml-auto hidden text-xs sm:inline">⌘K / Ctrl+K</kbd>
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent
          className="w-[calc(100vw-2rem)] gap-0 overflow-hidden p-0 sm:max-w-xl"
          showCloseButton={false}
          initialFocus={inputRef}
          finalFocus={() =>
            returnFocus.current?.isConnected
              ? returnFocus.current
              : triggerRef.current
          }
        >
          <DialogTitle className="sr-only">Поиск по справочникам</DialogTitle>
          <DialogDescription className="sr-only">
            Поиск подразделений, локаций и должностей. Стрелки перемещают
            выделение. Enter открывает подразделение. Escape закрывает поиск.
          </DialogDescription>
          {open && (
            <SearchContent inputRef={inputRef} onNavigate={() => setOpen(false)} />
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}

function SearchContent({
  inputRef,
  onNavigate,
}: {
  inputRef: React.RefObject<HTMLInputElement | null>;
  onNavigate: () => void;
}) {
  const router = useRouter();
  const [search, setSearch] = useState("");
  const query = search.trim();
  const [debouncedQuery] = useDebounce(query, 300);
  const isReady = query.length >= 2 && query === debouncedQuery;
  const result = useQuery({
    queryKey: ["global-search", query, SEARCH_PAGE_SIZE],
    queryFn: ({ signal }) => searchDirectory(query, signal),
    enabled: isReady,
    retry: false,
    staleTime: Infinity,
    gcTime: 0,
  });
  const data = isReady ? result.data : undefined;
  const isEmpty =
    data && groups.every(({ key }) => data[key].items.length === 0);

  return (
    <Command shouldFilter={false} label="Поисковый запрос">
      <CommandInput
        ref={inputRef}
        value={search}
        onValueChange={setSearch}
        aria-label="Поисковый запрос"
        placeholder="Название подразделения, локации или должности"
        className="text-sm"
      />
      <CommandList
        label="Результаты поиска"
        className="max-h-[min(60vh,28rem)] p-2"
      >
        <div role="status" aria-live="polite" className="text-muted-foreground">
          {query.length < 2 ? (
            <p className="p-4">Введите не менее двух символов</p>
          ) : !isReady || result.isPending || result.isFetching ? (
            <p className="flex items-center gap-2 p-4">
              <Spinner aria-hidden="true" />Ищем…
            </p>
          ) : isEmpty ? (
            <p className="p-4">Ничего не найдено</p>
          ) : null}
        </div>
        {isReady && result.isError ? (
          <div role="alert" className="space-y-3 p-4">
            <p>Не удалось выполнить поиск.</p>
            <Button
              variant="outline"
              size="sm"
              disabled={result.isFetching}
              onClick={() => void result.refetch()}
            >
              Попробовать снова
            </Button>
          </div>
        ) : (
          groups.map((group) =>
            data?.[group.key].items.length ? (
              <SearchResultsGroup
                key={`${query}:${group.key}`}
                group={group}
                query={query}
                firstPage={data[group.key]}
                onSelectDepartment={(id) => {
                  router.push(getDepartmentStructureHref(id));
                  onNavigate();
                }}
              />
            ) : null,
          )
        )}
      </CommandList>
      <p className="border-t px-4 py-3 text-xs text-muted-foreground">
        ↑ ↓ — выбрать · Enter — открыть подразделение · Esc — закрыть
      </p>
    </Command>
  );
}

function SearchResultsGroup({
  group: { key, title, icon: Icon, accent },
  query,
  firstPage,
  onSelectDepartment,
}: {
  group: (typeof groups)[number];
  query: string;
  firstPage: SearchPage;
  onSelectDepartment: (id: string) => void;
}) {
  const result = useInfiniteQuery({
    queryKey: ["global-search", query, SEARCH_PAGE_SIZE, key],
    queryFn: ({ pageParam, signal }) =>
      searchDirectoryPage(key, query, pageParam, signal),
    initialPageParam: 1,
    initialData: { pages: [firstPage], pageParams: [1] },
    getNextPageParam: (lastPage, pages) =>
      lastPage.hasMore ? pages.length + 1 : undefined,
    retry: false,
    staleTime: Infinity,
    gcTime: 0,
  });
  const items = [
    ...new Map(
      result.data.pages.flatMap((page) => page.items).map((item) => [item.id, item]),
    ).values(),
  ];

  return (
    <CommandGroup heading={title}>
      {items.map((item) => (
        <CommandItem
          key={item.id}
          value={`${key}:${item.id}`}
          onSelect={() => {
            if (key === "departments") onSelectDepartment(item.id);
          }}
          className={key === "departments" ? "cursor-pointer" : undefined}
        >
          <span
            className={`flex size-7 shrink-0 items-center justify-center ${accent}`}
          >
            <Icon className="size-4" weight="duotone" aria-hidden="true" />
          </span>
          <span className="min-w-0 break-words">{item.name}</span>
        </CommandItem>
      ))}
      {result.isFetchNextPageError && (
        <p role="alert" className="px-2 py-2 text-xs text-destructive">
          Не удалось загрузить следующие результаты.
        </p>
      )}
      {result.hasNextPage && (
        <CommandItem
          value={`${key}:load-more`}
          aria-label={`${result.isFetchNextPageError ? "Повторить загрузку" : "Загрузить ещё"}: ${title.toLowerCase()}`}
          disabled={result.isFetchingNextPage}
          onSelect={() => {
            if (!result.isFetchingNextPage) {
              void result.fetchNextPage({ cancelRefetch: false });
            }
          }}
          className="cursor-pointer justify-center text-muted-foreground"
        >
          {result.isFetchingNextPage && <Spinner aria-hidden="true" />}
          {result.isFetchingNextPage
            ? "Загружаем…"
            : result.isFetchNextPageError
              ? "Попробовать снова"
              : "Загрузить ещё"}
        </CommandItem>
      )}
    </CommandGroup>
  );
}
