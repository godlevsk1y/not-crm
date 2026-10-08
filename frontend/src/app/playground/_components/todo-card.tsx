import { cn } from "@/shared/lib/utils";
import { Button } from "@/shared/ui/button";
import { Card, CardContent } from "@/shared/ui/card";
import { Checkbox } from "@/shared/ui/checkbox";
import { CheckIcon, TrashIcon } from "@phosphor-icons/react";
import { TodoEntry } from "../page";

type TodoCardProps = {
  todo: TodoEntry;
  onToggle: (id: string, checked: boolean) => void;
  onDelete: (id: string) => void;
};

export default function TodoCard({ todo, onToggle, onDelete }: TodoCardProps) {
  return (
    <Card
      className={cn(
        "gap-0 rounded-xl py-0 shadow-none transition-colors",
        todo.completed
          ? "border-border/50 bg-muted/40"
          : "border-border/70 hover:border-stone-300 dark:hover:border-stone-600",
      )}
    >
      <CardContent className="flex items-start gap-4 p-5">
        <Checkbox
          id={`todo-${todo.id}`}
          checked={todo.completed}
          onCheckedChange={(checked: boolean) => onToggle(todo.id, checked)}
          className="mt-0.5 size-5 rounded-full data-checked:border-emerald-600 data-checked:bg-emerald-600 dark:data-checked:bg-emerald-600"
        />
        <div className="min-w-0 flex-1">
          <label
            htmlFor={`todo-${todo.id}`}
            className={cn(
              "block cursor-pointer text-sm leading-6 font-medium wrap-anywhere",
              todo.completed &&
                "text-muted-foreground line-through decoration-stone-400",
            )}
          >
            {todo.title}
          </label>
          {todo.description && (
            <p className="mt-1 text-sm leading-relaxed text-muted-foreground wrap-anywhere">
              {todo.description}
            </p>
          )}
        </div>
        {todo.completed && (
          <CheckIcon
            aria-hidden="true"
            className="mt-1 size-4 shrink-0 text-emerald-600"
          />
        )}
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label="Удалить задачу"
          className="rounded-lg text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
          onClick={() => onDelete(todo.id)}
        >
          <TrashIcon aria-hidden="true" />
        </Button>
      </CardContent>
    </Card>
  );
}
