import { MapPinIcon, TreeStructureIcon } from "@phosphor-icons/react/ssr";
import type { LocationListItemDto } from "@/entities/locations/types";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";

const dateFormatter = new Intl.DateTimeFormat("ru-RU", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});

const departmentPluralRules = new Intl.PluralRules("ru-RU");
const departmentLabels: Record<string, string> = {
  one: "подразделение",
  few: "подразделения",
  many: "подразделений",
  other: "подразделения",
};

export default function LocationCard({
  location,
}: {
  location: LocationListItemDto;
}) {
  const { name, address, departmentCount, createdAt } = location;
  const createdAtDate = new Date(createdAt);

  return (
    <Card className="h-full shadow-sm transition duration-200 hover:shadow-lg hover:ring-foreground/20 motion-safe:hover:-translate-y-1 [--card-spacing:--spacing(6)]">
      <CardHeader className="gap-3">
        <div className="mb-2 flex items-center justify-between gap-3">
          <span className="flex size-10 shrink-0 items-center justify-center bg-sky-500/10 text-sky-600 dark:text-sky-400">
            <MapPinIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <span className="min-w-0 wrap-break-words bg-muted px-3 py-1 text-xs text-muted-foreground">
            {address.city}
          </span>
        </div>
        <CardTitle className="text-lg font-semibold">
          <h3 className="wrap-break-words">{name}</h3>
        </CardTitle>
      </CardHeader>

      <CardContent className="flex-1">
        <address className="text-sm leading-relaxed not-italic">
          <p>
            {address.street}, {address.houseNumber}
          </p>
          <p className="mt-1 text-muted-foreground">
            {[address.district, address.city, address.region]
              .filter(Boolean)
              .join(", ")}
          </p>
          <p className="mt-1 text-muted-foreground">
            {[address.country, address.postalCode].filter(Boolean).join(", ")}
          </p>
        </address>
      </CardContent>

      <CardFooter className="mt-auto flex-col items-start gap-3">
        <p className="flex items-center gap-2 text-sm">
          <TreeStructureIcon
            size={18}
            className="shrink-0 text-muted-foreground"
            aria-hidden="true"
          />
          {departmentCount === 0 ? (
            <span className="text-muted-foreground">Нет подразделений</span>
          ) : (
            <span>
              <span className="font-semibold">{departmentCount}</span>{" "}
              {departmentLabels[departmentPluralRules.select(departmentCount)]}
            </span>
          )}
        </p>
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
