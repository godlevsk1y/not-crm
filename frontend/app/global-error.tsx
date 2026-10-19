"use client";

import { Geist, Geist_Mono, JetBrains_Mono } from "next/font/google";
import { cn } from "cn";
import "@/_app/styles/globals.css";
import { ROUTES } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import {
  ArrowClockwiseIcon,
  ArrowUpRightIcon,
  WarningCircleIcon,
} from "@phosphor-icons/react";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

const jetbrainsMono = JetBrains_Mono({
  variable: "--font-mono",
  subsets: ["latin"],
});

export default function GlobalError({
  error,
  retry,
}: {
  error: unknown;
  retry: () => void;
}) {
  console.error(error);

  return (
    <html
      lang="ru"
      className={cn(
        "h-full antialiased font-sans",
        geistSans.variable,
        geistMono.variable,
        jetbrainsMono.variable,
      )}
    >
      <body className="flex min-h-svh flex-col bg-background text-foreground">
        <header className="border-b px-6 py-5 sm:px-10">
          <span className="text-xs tracking-[0.2em] text-muted-foreground uppercase">
            notCRM
          </span>
        </header>

        <main className="flex flex-1 items-center justify-center px-6 py-12 sm:py-20">
          <section aria-labelledby="error-title" className="w-full max-w-lg">
            <div className="border bg-card p-6 text-card-foreground shadow-sm sm:p-10">
              <div className="mb-8 flex size-14 items-center justify-center bg-amber-500/10 text-amber-600 dark:text-amber-400">
                <WarningCircleIcon
                  size={30}
                  weight="duotone"
                  aria-hidden="true"
                />
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
                Не удалось отобразить страницу. Попробуйте ещё раз или вернитесь
                на главную.
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
                  render={<a href={ROUTES.HOME} />}
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

        <footer className="border-t px-6 py-5 text-xs text-muted-foreground sm:px-10">
          Справочник организации
        </footer>
      </body>
    </html>
  );
}
