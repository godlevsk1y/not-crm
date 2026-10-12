"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  BriefcaseIcon,
  BuildingsIcon,
  MapPinIcon,
  TreeStructureIcon,
} from "@phosphor-icons/react";
import { ROUTES } from "@/shared/config/routes";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  useSidebar,
} from "@/shared/ui/sidebar";
import SidebarSection from "./sidebar-section";

const sections = [
  {
    href: ROUTES.DEPARTMENTS,
    title: "Подразделения",
    icon: TreeStructureIcon,
    accent: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
  },
  {
    href: ROUTES.LOCATIONS,
    title: "Локации",
    icon: MapPinIcon,
    accent: "bg-sky-500/10 text-sky-600 dark:text-sky-400",
  },
  {
    href: ROUTES.POSITIONS,
    title: "Должности",
    icon: BriefcaseIcon,
    accent: "bg-violet-500/10 text-violet-600 dark:text-violet-400",
  },
] as const;

export function AppSidebar() {
  const pathname = usePathname();
  const { setOpenMobile } = useSidebar();

  return (
    <Sidebar collapsible="icon" aria-label="Навигация по сервисам">
      <SidebarHeader className="gap-2 border-b px-6 py-6 group-data-[collapsible=icon]:hidden">
        <p className="text-xs tracking-[0.2em] text-muted-foreground uppercase">
          notCRM
        </p>
        <p className="text-lg font-semibold tracking-tight">Список сервисов</p>
      </SidebarHeader>

      <SidebarContent className="px-3 py-5 group-data-[collapsible=icon]:px-2">
        <SidebarGroup className="gap-3 group-data-[collapsible=icon]:p-0">
          <SidebarGroupLabel className="px-2 text-[10px] tracking-[0.2em] uppercase">
            Сервисы
          </SidebarGroupLabel>
          <SidebarGroupContent>
            <nav aria-label="Сервисы">
              <SidebarMenu className="gap-4">
                <SidebarMenuItem>
                  <SidebarMenuButton
                    render={<Link href={ROUTES.HOME} />}
                    size="lg"
                    tooltip="Справочник подразделений"
                    aria-label="Справочник подразделений"
                    isActive={pathname === ROUTES.HOME}
                    aria-current={pathname === ROUTES.HOME ? "page" : undefined}
                    onClick={() => setOpenMobile(false)}
                    className="h-auto gap-3 border border-border/70 bg-background p-3 shadow-sm transition-colors hover:border-foreground/20 data-active:border-foreground/20 data-active:bg-background"
                  >
                    <span className="flex size-9 shrink-0 items-center justify-center bg-foreground/5 group-data-[collapsible=icon]:size-8">
                      <BuildingsIcon weight="duotone" aria-hidden="true" />
                    </span>
                    <span className="flex min-w-0 flex-col gap-1 group-data-[collapsible=icon]:hidden">
                      <span className="whitespace-normal text-sm font-semibold leading-snug">
                        Справочник подразделений
                      </span>
                      <span className="text-[10px] font-normal tracking-wide text-muted-foreground">
                        Directory Service
                      </span>
                    </span>
                  </SidebarMenuButton>

                  <SidebarMenuSub className="mr-0 mt-3 gap-1 border-border/80 pr-0 group-data-[collapsible=icon]:mx-0 group-data-[collapsible=icon]:flex group-data-[collapsible=icon]:translate-x-0 group-data-[collapsible=icon]:border-0 group-data-[collapsible=icon]:px-0">
                    {sections.map((section) => (
                      <SidebarSection key={section.href} {...section} />
                    ))}
                  </SidebarMenuSub>
                </SidebarMenuItem>
              </SidebarMenu>
            </nav>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter className="gap-1 border-t px-6 py-5 group-data-[collapsible=icon]:hidden">
        <p className="text-xs font-medium">Справочник организации</p>
        <p className="text-xs text-muted-foreground">
          Структура, места работы и должности
        </p>
      </SidebarFooter>
    </Sidebar>
  );
}
