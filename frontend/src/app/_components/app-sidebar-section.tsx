"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { Icon } from "@phosphor-icons/react";
import { cn } from "@/shared/lib/utils";
import {
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  useSidebar,
} from "@/shared/ui/sidebar";

type AppSidebarSectionProps = {
  href: string;
  title: string;
  icon: Icon;
  accent: string;
};

export default function AppSidebarSection({
  href,
  title,
  icon: Icon,
  accent,
}: AppSidebarSectionProps) {
  const pathname = usePathname();
  const { setOpenMobile } = useSidebar();
  const isActive = pathname === href || pathname.startsWith(`${href}/`);

  return (
    <SidebarMenuSubItem>
      <SidebarMenuSubButton
        render={<Link href={href} />}
        isActive={isActive}
        aria-current={isActive ? "page" : undefined}
        onClick={() => setOpenMobile(false)}
        className="h-11 gap-3 px-2 transition-colors data-active:bg-background data-active:font-semibold data-active:shadow-sm data-active:ring-1 data-active:ring-border"
      >
        <span
          className={cn(
            "flex size-7 shrink-0 items-center justify-center",
            accent,
          )}
        >
          <Icon size={17} weight="duotone" aria-hidden="true" />
        </span>
        <span>{title}</span>
      </SidebarMenuSubButton>
    </SidebarMenuSubItem>
  );
}
