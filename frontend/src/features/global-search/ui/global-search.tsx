"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useDebounce } from "use-debounce";
import { MagnifyingGlassIcon } from "@phosphor-icons/react";

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
import { searchDirectory } from "../api/search";

const groups = [
  { key: "departments", title: "Подразделения" },
  { key: "locations", title: "Локации" },
  { key: "positions", title: "Должности" },
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
    queryKey: ["global-search", query],
    queryFn: ({ signal }) => searchDirectory(query, signal),
    enabled: isReady,
    retry: false,
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
          groups.map(({ key, title }) =>
            data?.[key].items.length ? (
              <CommandGroup key={key} heading={title}>
                {data[key].items.map((item) => (
                  <CommandItem
                    key={item.id}
                    value={`${key}:${item.id}`}
                    onSelect={() => {
                      if (key !== "departments") return;
                      router.push(getDepartmentStructureHref(item.id));
                      onNavigate();
                    }}
                    className={key === "departments" ? "cursor-pointer" : undefined}
                  >
                    <span className="min-w-0 break-words">{item.name}</span>
                  </CommandItem>
                ))}
              </CommandGroup>
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
