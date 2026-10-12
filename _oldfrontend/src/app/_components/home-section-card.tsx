import Link from "next/link";
import type { Icon } from "@phosphor-icons/react";
import { ArrowUpRightIcon } from "@phosphor-icons/react/ssr";
import { cn } from "@/shared/lib/utils";
import {
  Card,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";

type HomeSectionCardProps = {
  href: string;
  title: string;
  description: string;
  icon: Icon;
  accent: string;
};

export default function HomeSectionCard({
  href,
  title,
  description,
  icon: Icon,
  accent,
}: HomeSectionCardProps) {
  return (
    <Link
      href={href}
      className="group outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-4"
    >
      <Card className="h-full shadow-sm transition duration-200 group-hover:shadow-lg group-hover:ring-foreground/20 motion-safe:group-hover:-translate-y-1 group-focus-visible:ring-foreground/20 [--card-spacing:--spacing(6)]">
        <CardHeader className="gap-3">
          <div
            className={cn(
              "mb-5 flex size-12 items-center justify-center",
              accent,
            )}
          >
            <Icon size={26} weight="duotone" aria-hidden="true" />
          </div>
          <CardTitle className="text-lg font-semibold">{title}</CardTitle>
          <CardDescription className="text-sm">{description}</CardDescription>
        </CardHeader>
        <CardFooter className="mt-auto justify-between text-muted-foreground group-hover:text-foreground">
          <span>Открыть раздел</span>
          <ArrowUpRightIcon
            size={18}
            aria-hidden="true"
            className="transition-transform motion-safe:group-hover:translate-x-0.5 motion-safe:group-hover:-translate-y-0.5"
          />
        </CardFooter>
      </Card>
    </Link>
  );
}
