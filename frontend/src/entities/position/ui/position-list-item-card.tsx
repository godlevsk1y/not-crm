import { BriefcaseIcon } from "@phosphor-icons/react/ssr";
import {
  Card,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";
import type { PositionListItemDto } from "../model/types";

const dateFormatter = new Intl.DateTimeFormat("ru-RU", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});

export function PositionListItemCard({
  position,
}: {
  position: PositionListItemDto;
}) {
  const { name, createdAt } = position;
  const createdAtDate = new Date(createdAt);

  return (
    <Card className="h-full shadow-sm transition duration-200 hover:shadow-lg hover:ring-foreground/20 motion-safe:hover:-translate-y-1 [--card-spacing:--spacing(6)]">
      <CardHeader className="gap-3">
        <span className="mb-2 flex size-10 shrink-0 items-center justify-center bg-violet-500/10 text-violet-600 dark:text-violet-400">
          <BriefcaseIcon size={24} weight="duotone" aria-hidden="true" />
        </span>
        <CardTitle className="text-lg font-semibold">
          <h3 className="wrap-break-words">{name}</h3>
        </CardTitle>
      </CardHeader>

      <CardFooter className="mt-auto">
        <p className="text-xs text-muted-foreground">
          Добавлена{" "}
          <time dateTime={createdAtDate.toISOString()}>
            {dateFormatter.format(createdAtDate)}
          </time>
        </p>
      </CardFooter>
    </Card>
  );
}
