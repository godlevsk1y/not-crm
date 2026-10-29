import { Button } from "@/shared/ui/button";

type StatusFilterProps =
  | {
      value: boolean;
      onChange: (isActive: boolean) => void;
      allowAll?: false;
    }
  | {
      value: boolean | undefined;
      onChange: (isActive: boolean | undefined) => void;
      allowAll: true;
    };

export function StatusFilter(props: StatusFilterProps) {
  return (
    <div className="grid gap-2">
      <span className="text-xs font-medium">Статус</span>
      <div role="group" aria-label="Статус" className="flex flex-wrap gap-1">
        {[
          { value: true, label: "Активный" },
          { value: false, label: "Архивный" },
        ].map((option) => (
          <Button
            key={option.label}
            type="button"
            variant={props.value === option.value ? "secondary" : "outline"}
            aria-pressed={props.value === option.value}
            onClick={() => props.onChange(option.value)}
          >
            {option.label}
          </Button>
        ))}
        {props.allowAll && (
          <Button
            type="button"
            variant={props.value === undefined ? "secondary" : "outline"}
            aria-pressed={props.value === undefined}
            onClick={() => props.onChange(undefined)}
          >
            Все
          </Button>
        )}
      </div>
    </div>
  );
}
