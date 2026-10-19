"use client";

import { ROUTES } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import {
  ArrowClockwiseIcon,
  ArrowUpRightIcon,
  WarningCircleIcon,
} from "@phosphor-icons/react";
import Link from "next/link";

export default function AppError({
  error,
  retry,
}: {
  error: unknown;
  retry: () => void;
}) {
  return (
    <main className="flex flex-1 items-center justify-center px-6 py-12 sm:py-20">
      <section aria-labelledby="error-title" className="w-full max-w-lg">
        <div className="border bg-card p-6 text-card-foreground shadow-sm sm:p-10">
          <div className="mb-8 flex size-14 items-center justify-center bg-amber-500/10 text-amber-600 dark:text-amber-400">
            <WarningCircleIcon size={30} weight="duotone" aria-hidden="true" />
          </div>

          <p className="mb-3 text-xs tracking-[0.2em] text-muted-foreground uppercase">
            Неожиданная ошибка
          </p>
          <h1
            id="error-title"
            className="text-2xl font-bold tracking-tight sm:text-3xl"
          >
            Что-то пошло не так
          </h1>
          <p className="mt-4 text-sm leading-relaxed text-muted-foreground">
            Не удалось отобразить страницу. Попробуйте ещё раз или вернитесь на
            главную.
          </p>

          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Button size="lg" onClick={() => retry()}>
              <ArrowClockwiseIcon aria-hidden="true" />
              Попробовать снова
            </Button>
            <Button
              variant="outline"
              size="lg"
              nativeButton={false}
              render={<Link href={ROUTES.HOME} />}
            >
              На главную
              <ArrowUpRightIcon aria-hidden="true" />
            </Button>
          </div>
        </div>

        <p className="mt-5 text-xs leading-relaxed text-muted-foreground">
          Если ошибка повторяется, попробуйте зайти позже.
        </p>
      </section>
    </main>
  );
}
